using FluentValidation.Results;

namespace CashFlow.Application.Common.Exceptions;

/// <summary>
/// Lançada pelo <c>ValidationBehavior</c> quando uma ou mais regras do
/// FluentValidation falham para um comando/consulta de entrada. A camada
/// de Api mapeia isso para HTTP 400 (Bad Request) com um payload de erros
/// por campo.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationFailure> failures) : base("Ocorreram uma ou mais falhas de validação.")
    {
        Errors = failures
            .GroupBy(failure => failure.PropertyName, failure => failure.ErrorMessage)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
