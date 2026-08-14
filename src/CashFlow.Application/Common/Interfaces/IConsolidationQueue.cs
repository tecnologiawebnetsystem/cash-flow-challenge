namespace CashFlow.Application.Common.Interfaces;

/// <summary>
/// Producer/consumer abstraction that decouples launch registration from
/// daily balance consolidation. This is the core mechanism behind the
/// non-functional resilience requirement: registering a launch never
/// waits on - or fails because of - the consolidation subsystem.
///
/// The queue is best-effort and bounded: under extreme burst load it may
/// drop a "please consolidate this date" signal (see <see cref="TryEnqueue"/>
/// return value), but that never loses financial data, because launches are
/// always persisted synchronously regardless of the queue outcome, and
/// consolidation itself is idempotent and periodically reconciled.
/// </summary>
public interface IConsolidationQueue
{
    /// <summary>
    /// Attempts to schedule a consolidation for <paramref name="date"/>.
    /// Returns <c>false</c> when the queue is full and the request was
    /// dropped instead of blocking the caller.
    /// </summary>
    bool TryEnqueue(DateOnly date);

    IAsyncEnumerable<DateOnly> ReadAllAsync(CancellationToken cancellationToken);
}
