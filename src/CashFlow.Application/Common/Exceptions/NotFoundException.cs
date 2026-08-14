namespace CashFlow.Application.Common.Exceptions;

/// <summary>
/// Lançada quando um recurso solicitado não existe. Mapeada para HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException ForDailyBalance(DateOnly date) =>
        new($"Ainda não há saldo diário consolidado para {date:yyyy-MM-dd}.");
}
