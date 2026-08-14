using CashFlow.Application.Common.Interfaces;

namespace CashFlow.Infrastructure;

/// <summary>
/// Wraps <see cref="DateTime"/>/<see cref="DateOnly"/> statics behind an
/// interface so Application code (and its unit tests) never depend on the
/// system clock directly.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
