namespace CashFlow.Application.Common.Interfaces;

/// <summary>
/// Persists that consolidation failed for a given date after every retry
/// attempt (and the circuit breaker, if applicable) was exhausted. Kept as
/// its own narrow interface (Interface Segregation) so the background
/// worker's failure path does not need the full command/query surface.
/// </summary>
public interface IConsolidationFailureRecorder
{
    Task RecordFailureAsync(DateOnly date, string reason, CancellationToken cancellationToken);
}
