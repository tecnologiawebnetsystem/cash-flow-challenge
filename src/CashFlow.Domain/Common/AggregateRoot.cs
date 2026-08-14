using CashFlow.Domain.Events;

namespace CashFlow.Domain.Common;

/// <summary>
/// Classe base para raízes de agregado (aggregate roots). Uma raiz de
/// agregado é o único ponto de entrada pelo qual seu agregado deve ser
/// modificado, e é responsável por registrar os eventos de domínio que
/// ocorreram como consequência do seu comportamento.
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
