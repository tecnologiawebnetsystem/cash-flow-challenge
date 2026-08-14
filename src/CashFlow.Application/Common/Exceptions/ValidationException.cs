using FluentValidation.Results;

namespace CashFlow.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>ValidationBehavior</c> when one or more FluentValidation
/// rules fail for an incoming command/query. The API layer maps this to
/// HTTP 400 (Bad Request) with a field-level error payload.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationFailure> failures) : base("One or more validation failures occurred.")
    {
        Errors = failures
            .GroupBy(failure => failure.PropertyName, failure => failure.ErrorMessage)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
