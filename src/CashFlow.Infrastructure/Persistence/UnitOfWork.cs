using CashFlow.Application.Common.Interfaces;

namespace CashFlow.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly CashFlowDbContext _context;

    public UnitOfWork(CashFlowDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
