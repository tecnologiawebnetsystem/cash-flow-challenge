namespace CashFlow.Application.Common.Interfaces;

/// <summary>
/// Abstração sobre o tempo do sistema para que os handlers permaneçam
/// determinísticos e testáveis (sem chamadas diretas a DateTime.UtcNow /
/// DateTime.Today espalhadas pelo código).
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateOnly Today { get; }
}
