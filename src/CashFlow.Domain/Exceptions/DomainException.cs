namespace CashFlow.Domain.Exceptions;

/// <summary>
/// Base type for every exception raised because a domain invariant was violated.
/// The API layer maps this family to HTTP 422 (Unprocessable Entity).
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
