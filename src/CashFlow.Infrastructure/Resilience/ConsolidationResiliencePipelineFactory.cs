using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace CashFlow.Infrastructure.Resilience;

/// <summary>
/// Builds the resilience pipeline applied around every consolidation
/// attempt. This is where the non-functional resilience requirement is
/// implemented in infrastructure code, kept entirely out of the
/// Application/Domain business logic (Single Responsibility Principle):
///
///   - Retry: transient failures (e.g. a momentary database hiccup) are
///     retried with exponential backoff + jitter instead of failing the
///     whole batch immediately.
///   - Circuit breaker: if failures persist, the breaker opens and fails
///     fast for a cool-down window instead of piling up retries against an
///     already struggling dependency, giving it room to recover.
///
/// Because <see cref="ConsolidateDailyBalanceCommandHandler"/> is fully
/// idempotent, retrying (or replaying later via reconciliation) is always
/// safe - it never double-counts a launch.
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
