namespace CashFlow.Domain.Exceptions;

public sealed class InvalidLaunchDescriptionException : DomainException
{
    public InvalidLaunchDescriptionException()
        : base("A descrição do lançamento não pode ser vazia e deve ter no máximo 200 caracteres.")
    {
    }
}
