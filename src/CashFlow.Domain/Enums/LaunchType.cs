namespace CashFlow.Domain.Enums;

/// <summary>
/// A natureza de um lançamento de fluxo de caixa: um crédito aumenta o
/// saldo, um débito o diminui.
/// </summary>
public enum LaunchType
{
    Credit = 1,
    Debit = 2
}
