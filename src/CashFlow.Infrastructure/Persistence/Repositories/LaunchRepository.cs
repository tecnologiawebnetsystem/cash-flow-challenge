using CashFlow.Domain.Entities;
using CashFlow.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Persistence.Repositories;

public sealed class LaunchRepository : ILaunchRepository
{
    private readonly CashFlowDbContext _context;

    public LaunchRepository(CashFlowDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Launch launch, CancellationToken cancellationToken)
    {
        await _context.Launches.AddAsync(launch, cancellationToken);
    }

    public async Task<Launch?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Launches
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Launch>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken)
    {
        return await _context.Launches
            .Where(l => l.LaunchDate == date)
            .OrderBy(l => l.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Launch>> GetByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        return await _context.Launches
            .Where(l => l.LaunchDate >= startDate && l.LaunchDate <= endDate)
            .OrderBy(l => l.LaunchDate)
            .ThenBy(l => l.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
