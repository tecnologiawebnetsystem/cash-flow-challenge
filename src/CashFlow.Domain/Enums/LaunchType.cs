namespace CashFlow.Domain.Enums;

/// <summary>
/// The nature of a cash flow launch (lançamento): a credit increases the
/// balance, a debit decreases it.
/// </summary>
public enum LaunchType
{
    Credit = 1,
    Debit = 2
}
