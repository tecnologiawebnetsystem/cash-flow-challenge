using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace CashFlow.Infrastructure.Resilience;

/// <summary>
/// Constrói o pipeline de resiliência aplicado em torno de cada tentativa
/// de consolidação. É aqui que o requisito não funcional de resiliência é
/// implementado em código de infraestrutura, mantido totalmente fora da
/// lógica de negócio de Application/Domain (Princípio da Responsabilidade
/// Única):
///
///   - Retry: falhas transitórias (ex.: uma instabilidade momentânea no
///     banco de dados) são reexecutadas com backoff exponencial + jitter em
///     vez de falhar o lote inteiro imediatamente.
///   - Circuit breaker: se as falhas persistirem, o circuito abre e falha
///     rapidamente (fail fast) durante uma janela de resfriamento, em vez
///     de acumular retries contra uma dependência já sobrecarregada,
///     dando a ela espaço para se recuperar.
///
/// Como <see cref="ConsolidateDailyBalanceCommandHandler"/> é totalmente
/// idempotente, reexecutar (ou reprocessar depois via reconciliação) é
/// sempre seguro - nunca conta um lançamento em duplicidade.
/// </summary>
public static class ConsolidationResiliencePipelineFactory
{
    public static ResiliencePipeline Create(ILogger logger)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    logger.LogWarning(
                        args.Outcome.Exception,
                        "Retry {AttemptNumber} for daily balance consolidation after failure.",
                        args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                },
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                FailureRatio = 0.5,
                MinimumThroughput = 4,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15),
                OnOpened = args =>
                {
                    logger.LogError(
                        "Consolidation circuit breaker opened for {BreakDuration}. " +
                        "Consolidation signals will fail fast until it closes again; " +
                        "launch registration remains unaffected.",
                        args.BreakDuration);
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    logger.LogInformation("Consolidation circuit breaker closed; resuming normal processing.");
                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }
}
