using CashFlow.Domain.Entities;

namespace CashFlow.Domain.Repositories;


public interface ILaunchRepository
{
    Task AddAsync(Launch launch, CancellationToken cancellationToken);

    Task<Launch?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Launch>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyList<Launch>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);
}
