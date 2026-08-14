using CashFlow.Domain.Entities;
using CashFlow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Persistence.Repositories;

public sealed class DailyBalanceRepository : IDailyBalanceRepository
{
    private readonly CashFlowDbContext _context;

    public DailyBalanceRepository(CashFlowDbContext context)
    {
        _context = context;
    }

    public async Task<DailyBalance?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken)
    {
        return await _context.DailyBalances
            .SingleOrDefaultAsync(b => b.ReferenceDate == date, cancellationToken);
    }

    public async Task<IReadOnlyList<DailyBalance>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        return await _context.DailyBalances
            .Where(b => b.ReferenceDate >= startDate && b.ReferenceDate <= endDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task UpsertAsync(DailyBalance dailyBalance, CancellationToken cancellationToken)
    {
        var isTracked = _context.ChangeTracker
            .Entries<DailyBalance>()
            .Any(e => e.Entity.Id == dailyBalance.Id);

        if (!isTracked)
        {
            _context.DailyBalances.Add(dailyBalance);
        }

        return Task.CompletedTask;
    }
}
