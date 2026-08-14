using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Events;

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
