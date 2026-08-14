using CashFlow.Application.Common.Interfaces;
using CashFlow.Application.Consolidation.Commands.ConsolidateDailyBalance;
using CashFlow.Infrastructure.Resilience;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace CashFlow.Infrastructure.BackgroundServices;

/// <summary>
/// Single background consumer of the consolidation queue. Runs entirely out
/// of the request path of <c>POST /launches</c>, which is the mechanism
/// behind the "the launch subsystem must stay up even if consolidation
/// fails" non-functional requirement.
///
/// Each dequeued date is processed through a resilience pipeline
/// (retry + circuit breaker, see <see cref="ConsolidationResiliencePipelineFactory"/>).
/// If every attempt is exhausted or the circuit is open, the failure is
/// recorded on the corresponding <c>DailyBalance</c> row instead of being
/// silently lost, and <see cref="ReconciliationWorker"/> will retry it on
/// its next pass.
/// </summary>
public sealed class ConsolidationWorker : BackgroundService
{
    private readonly IConsolidationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConsolidationWorker> _logger;
    private readonly ResiliencePipeline _pipeline;

    public ConsolidationWorker(
        IConsolidationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ConsolidationWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pipeline = ConsolidationResiliencePipelineFactory.Create(logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var date in _queue.ReadAllAsync(stoppingToken))
            {
                await ProcessAsync(date, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during graceful shutdown.
        }
    }

    private async Task ProcessAsync(DateOnly date, CancellationToken stoppingToken)
    {
        try
        {
            await _pipeline.ExecuteAsync(
                async token =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
                    await mediator.Send(new ConsolidateDailyBalanceCommand(date), token);
                },
                stoppingToken);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError(
                "Consolidation for {Date} skipped: circuit breaker is open. " +
                "The periodic reconciliation job will retry it once the breaker recovers.",
                date);

            await RecordFailureAsync(date, "Circuit breaker open.", stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Consolidation for {Date} failed after all retry attempts.", date);

            await RecordFailureAsync(date, ex.Message, stoppingToken);
        }
    }

    private async Task RecordFailureAsync(DateOnly date, string reason, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var recorder = scope.ServiceProvider.GetRequiredService<IConsolidationFailureRecorder>();
            await recorder.RecordFailureAsync(date, reason, cancellationToken);
        }
        catch (Exception ex)
        {
            // Recording the failure is itself best-effort: if even this
            // fails, the periodic reconciliation job is the final safety
            // net, since it does not depend on a Failed row existing.
            _logger.LogError(ex, "Failed to persist failure state for {Date}.", date);
        }
    }
}
