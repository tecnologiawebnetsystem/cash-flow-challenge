namespace CashFlow.Domain.Exceptions;

public sealed class InvalidLaunchAmountException : DomainException
{
    public InvalidLaunchAmountException(decimal amount)
        : base($"Launch amount must be greater than zero. Received: {amount}.")
    {
    }
}
