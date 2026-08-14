using CashFlow.Domain.Common;
using CashFlow.Domain.Exceptions;

namespace CashFlow.Domain.ValueObjects;

/// <summary>
/// Represents the monetary amount of a single launch (lançamento).
/// Immutable and self-validating: it is impossible to construct a
/// <see cref="Money"/> instance with a value that is zero or negative,
/// which keeps that invariant in a single place instead of scattered
/// across the codebase.
///
/// This type intentionally has no arithmetic operators (Add/Subtract):
/// aggregated totals such as a daily balance's credits, debits and closing
/// balance are legitimately zero or negative, so they are modeled as plain
/// <see cref="decimal"/> on <see cref="Entities.DailyBalance"/> instead of
/// forcing this stricter invariant onto them.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; private init; }

    // Required by the EF Core materializer for the owned-type mapping.
    private Money()
    {
    }

    private Money(decimal amount)
    {
        Amount = amount;
    }

    public static Money Create(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidLaunchAmountException(amount);
        }

        // Guard against floating rounding surprises by normalizing to 2 decimal places.
        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }

    public override string ToString() => Amount.ToString("F2");
}
