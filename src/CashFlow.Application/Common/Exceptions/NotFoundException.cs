namespace CashFlow.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist. Mapped to HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException ForDailyBalance(DateOnly date) =>
        new($"No daily balance has been consolidated yet for {date:yyyy-MM-dd}.");
}
