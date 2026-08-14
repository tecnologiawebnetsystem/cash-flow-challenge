using CashFlow.Application.Common.Interfaces;
using CashFlow.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CashFlow.Infrastructure.BackgroundServices;

/// <summary>
/// Rede de segurança periódica que fecha a lacuna deixada pela fila de
/// consolidação, que é best-effort e limitada: sob o pico de carga
/// documentado (50 req/s, com até 5% de perda aceitável) um *sinal* de
/// consolidação pode legitimamente ser descartado, e uma falha transitória
/// pode esgotar todas as tentativas de retry.
///
/// Este worker reenfileira a consolidação para qualquer data que tenha
/// lançamentos mas ainda não tenha um saldo diário <c>Consolidated</c> bem-
/// sucedido - independentemente do motivo pelo qual esteja faltando. Como a
/// consolidação é idempotente, isso é sempre seguro de executar, mesmo que
/// acabe reprocessando uma data sem necessidade.
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
