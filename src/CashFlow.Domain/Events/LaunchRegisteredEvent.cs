using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Events;

/// <summary>
/// Disparado sempre que um novo lançamento financeiro é registrado.
/// Consumidores (ex.: o subsistema de consolidação) reagem a este evento
/// sem que o agregado Launch saiba nada sobre eles (DIP).
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
