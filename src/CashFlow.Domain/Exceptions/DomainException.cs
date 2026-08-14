namespace CashFlow.Domain.Exceptions;

/// <summary>
/// Tipo base para toda exceção lançada por violação de um invariante de
/// domínio. A camada de Api mapeia esta família para HTTP 422
/// (Unprocessable Entity).
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
