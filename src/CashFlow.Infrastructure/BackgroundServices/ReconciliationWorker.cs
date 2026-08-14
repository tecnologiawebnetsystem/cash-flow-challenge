using CashFlow.Application.Common.Interfaces;
using CashFlow.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CashFlow.Infrastructure.BackgroundServices;

/// <summary>
/// Periodic safety net that closes the gap left by the best-effort,
/// bounded consolidation queue: under the documented peak load (50 req/s,
/// up to 5% acceptable loss) a consolidation *signal* can legitimately be
/// dropped, and a transient failure can exhaust every retry.
///
/// This worker re-enqueues consolidation for any date that has launches but
/// no successful <c>Consolidated</c> daily balance yet - regardless of why
/// it is missing. Because consolidation is idempotent, this is always safe
/// to run, even if it ends up re-processing a date unnecessarily.
/// </summary>
public sealed class ReconciliationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LookbackWindow = TimeSpan.FromDays(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConsolidationQueue _queue;
    private readonly ILogger<ReconciliationWorker> _logger;

    public ReconciliationWorker(
        IServiceScopeFactory scopeFactory,
        IConsolidationQueue queue,
        ILogger<ReconciliationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconciliation pass failed; will retry on the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var launchRepository = scope.ServiceProvider.GetRequiredService<ILaunchRepository>();
        var dailyBalanceRepository = scope.ServiceProvider.GetRequiredService<IDailyBalanceRepository>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var today = dateTimeProvider.Today;
        var startDate = today.AddDays(-LookbackWindow.Days);

        var launches = await launchRepository.GetByDateRangeAsync(startDate, today, cancellationToken);
        var datesWithLaunches = launches.Select(l => l.LaunchDate).Distinct().ToHashSet();

        if (datesWithLaunches.Count == 0)
        {
            return;
        }

        var balances = await dailyBalanceRepository.GetByDateRangeAsync(startDate, today, cancellationToken);
        var consolidatedDates = balances
            .Where(b => b.Status == Domain.Enums.ConsolidationStatus.Consolidated)
            .Select(b => b.ReferenceDate)
            .ToHashSet();

        var pendingDates = datesWithLaunches.Except(consolidatedDates).ToList();

        foreach (var date in pendingDates)
        {
            if (_queue.TryEnqueue(date))
            {
                _logger.LogInformation("Reconciliation re-enqueued consolidation for {Date}.", date);
            }
        }
    }
}
