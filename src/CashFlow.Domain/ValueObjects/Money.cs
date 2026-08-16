using CashFlow.Domain.Common;
using CashFlow.Domain.Exceptions;

namespace CashFlow.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; private init; }

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

        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }

    public override string ToString() => Amount.ToString("F2");
}
