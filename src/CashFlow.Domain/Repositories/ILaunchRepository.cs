using CashFlow.Domain.Entities;

namespace CashFlow.Domain.Repositories;

/// <summary>
/// Persistence abstraction for <see cref="Launch"/> aggregates.
/// The Domain and Application layers depend only on this interface
/// (Dependency Inversion Principle); Infrastructure provides the
/// EF Core / PostgreSQL implementation.
/// </summary>
public interface ILaunchRepository
{
    Task AddAsync(Launch launch, CancellationToken cancellationToken);

    Task<Launch?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Launch>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyList<Launch>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);
}
