namespace CashFlow.Application.Common.Interfaces;

/// <summary>
/// Abstraction over system time so handlers stay deterministic and testable
/// (no direct calls to DateTime.UtcNow / DateTime.Today scattered around).
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateOnly Today { get; }
}
