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
/// Único consumidor em background da fila de consolidação. Executa
/// totalmente fora do caminho da requisição de <c>POST /launches</c>, que é
/// o mecanismo por trás do requisito não funcional "o subsistema de
/// lançamentos deve permanecer disponível mesmo se a consolidação falhar".
///
/// Cada data retirada da fila é processada por um pipeline de resiliência
/// (retry + circuit breaker, ver <see cref="ConsolidationResiliencePipelineFactory"/>).
/// Se todas as tentativas se esgotarem ou o circuito estiver aberto, a
/// falha é registrada na linha correspondente de <c>DailyBalance</c> em vez
/// de ser silenciosamente perdida, e o <see cref="ReconciliationWorker"/>
/// tentará novamente na sua próxima passada.
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
            // Esperado durante o encerramento gracioso (graceful shutdown).
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
            // Registrar a falha também é best-effort: se até isso falhar,
            // o job periódico de reconciliação é a rede de segurança final,
            // já que ele não depende da existência de uma linha Failed.
            _logger.LogError(ex, "Failed to persist failure state for {Date}.", date);
        }
    }
}
