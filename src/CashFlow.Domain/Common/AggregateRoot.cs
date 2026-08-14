using CashFlow.Domain.Events;

namespace CashFlow.Domain.Common;

/// <summary>
/// Base class for aggregate roots. An aggregate root is the only entry point
/// through which its aggregate should be modified, and it is responsible for
/// recording the domain events that occurred as a consequence of its behavior.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(Guid id) : base(id)
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
