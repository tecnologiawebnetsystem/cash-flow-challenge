using CashFlow.Domain.Common;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Events;
using CashFlow.Domain.Exceptions;
using CashFlow.Domain.ValueObjects;

namespace CashFlow.Domain.Entities;

public sealed class Launch : AggregateRoot
{
    public string Description { get; private set; }
    public Money Amount { get; private set; }
    public LaunchType Type { get; private set; }
    public DateOnly LaunchDate { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    // Necessário para materialização pelo EF Core.
    private Launch()
    {
        Description = string.Empty;
        Amount = null!;
    }

    private Launch(Guid id, string description, Money amount, LaunchType type, DateOnly launchDate, DateTime createdAtUtc)
        : base(id)
    {
        Description = description;
        Amount = amount;
        Type = type;
        LaunchDate = launchDate;
        CreatedAtUtc = createdAtUtc;
    }

    public static Launch Create(string description, decimal amount, LaunchType type, DateOnly launchDate, DateTime nowUtc)
    {
        var normalizedDescription = (description ?? string.Empty).Trim();
        if (normalizedDescription.Length == 0 || normalizedDescription.Length > 200)
        {
            throw new InvalidLaunchDescriptionException();
        }

        var money = Money.Create(amount);

        var launch = new Launch(Guid.NewGuid(), normalizedDescription, money, type, launchDate, nowUtc);
        launch.RaiseDomainEvent(new LaunchRegisteredEvent(launch.Id, launch.LaunchDate, launch.Amount.Amount, launch.Type));

        return launch;
    }
    public decimal SignedAmount => Type == LaunchType.Credit ? Amount.Amount : -Amount.Amount;
}
