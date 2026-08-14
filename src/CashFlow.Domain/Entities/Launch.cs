using CashFlow.Domain.Common;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Events;
using CashFlow.Domain.Exceptions;
using CashFlow.Domain.ValueObjects;

namespace CashFlow.Domain.Entities;

/// <summary>
/// A single financial launch (lançamento): a credit or a debit that happened
/// on a given date. This is the single source of truth for the cash flow -
/// daily balances are always a derived/consolidated projection of launches,
/// never the other way around.
/// </summary>
public sealed class Launch : AggregateRoot
{
    public string Description { get; private set; }
    public Money Amount { get; private set; }
    public LaunchType Type { get; private set; }
    public DateOnly LaunchDate { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    // Required by the EF Core materializer.
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

    /// <summary>
    /// Factory method (encapsulates invariants and guarantees a Launch can
    /// never exist in an invalid state) that creates a new launch and raises
    /// a <see cref="LaunchRegisteredEvent"/> for interested subscribers
    /// (e.g. the consolidation subsystem) to react to.
    /// </summary>
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

    /// <summary>
    /// The signed contribution of this launch to a daily balance:
    /// positive for credits, negative for debits.
    /// </summary>
    public decimal SignedAmount => Type == LaunchType.Credit ? Amount.Amount : -Amount.Amount;
}
