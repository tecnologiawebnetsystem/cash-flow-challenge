namespace CashFlow.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
