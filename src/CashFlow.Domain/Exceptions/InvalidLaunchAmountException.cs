namespace CashFlow.Domain.Exceptions;

public sealed class InvalidLaunchAmountException : DomainException
{
    public InvalidLaunchAmountException(decimal amount)
        : base($"O valor do lançamento deve ser maior que zero. Recebido: {amount}.")
    {
    }
}
