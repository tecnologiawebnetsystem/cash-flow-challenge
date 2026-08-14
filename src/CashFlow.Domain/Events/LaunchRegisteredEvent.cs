using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Events;

/// <summary>
/// Raised whenever a new financial launch (lançamento) is registered.
/// Consumers (e.g. the consolidation subsystem) react to this event
/// without the Launch aggregate knowing anything about them (DIP).
/// </summary>
public sealed class LaunchRegisteredEvent : IDomainEvent
{
    public LaunchRegisteredEvent(Guid launchId, DateOnly launchDate, decimal amount, LaunchType type)
    {
        LaunchId = launchId;
        LaunchDate = launchDate;
        Amount = amount;
        Type = type;
        OccurredOnUtc = DateTime.UtcNow;
    }

    public Guid LaunchId { get; }
    public DateOnly LaunchDate { get; }
    public decimal Amount { get; }
    public LaunchType Type { get; }
    public DateTime OccurredOnUtc { get; }
}
