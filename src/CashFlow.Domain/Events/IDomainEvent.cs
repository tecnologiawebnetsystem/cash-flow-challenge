namespace CashFlow.Domain.Events;

/// <summary>
/// Interface marcadora para eventos de domínio. Mantida livre de dependências
/// (sem referência ao MediatR) para que a camada de Domain permaneça
/// agnóstica de framework, conforme as regras da Clean Architecture.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
