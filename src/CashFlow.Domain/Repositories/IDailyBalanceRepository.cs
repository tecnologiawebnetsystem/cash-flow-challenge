using CashFlow.Domain.Entities;

namespace CashFlow.Domain.Repositories;


public interface IDailyBalanceRepository
{
    Task<DailyBalance?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyList<DailyBalance>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

    Task UpsertAsync(DailyBalance dailyBalance, CancellationToken cancellationToken);
}
