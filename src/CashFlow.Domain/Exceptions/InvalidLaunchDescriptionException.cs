namespace CashFlow.Domain.Exceptions;

public sealed class InvalidLaunchDescriptionException : DomainException
{
    public InvalidLaunchDescriptionException()
        : base("Launch description must not be empty and must be at most 200 characters long.")
    {
    }
}
