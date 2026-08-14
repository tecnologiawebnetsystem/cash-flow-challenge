using CashFlow.Domain.Entities;

namespace CashFlow.Domain.Repositories;

/// <summary>
/// Persistence abstraction for <see cref="DailyBalance"/> projections.
/// </summary>
public interface IDailyBalanceRepository
{
    Task<DailyBalance?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyList<DailyBalance>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the daily balance if none exists for its date yet, or updates
    /// the existing one otherwise. Consolidation is idempotent by design.
    /// </summary>
    Task UpsertAsync(DailyBalance dailyBalance, CancellationToken cancellationToken);
}
