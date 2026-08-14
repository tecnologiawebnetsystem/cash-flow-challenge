namespace CashFlow.Domain.Events;

/// <summary>
/// Marker interface for domain events. Kept dependency-free (no MediatR reference)
/// so the Domain layer remains framework-agnostic, per Clean Architecture rules.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
