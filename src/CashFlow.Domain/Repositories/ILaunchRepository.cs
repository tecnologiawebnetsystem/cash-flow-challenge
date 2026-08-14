using CashFlow.Domain.Entities;

namespace CashFlow.Domain.Repositories;

/// <summary>
/// Abstração de persistência para os agregados <see cref="Launch"/>.
/// As camadas de Domain e Application dependem apenas desta interface
/// (Princípio da Inversão de Dependência); a Infrastructure fornece a
/// implementação com EF Core / PostgreSQL.
/// </summary>
public interface ILaunchRepository
{
    Task AddAsync(Launch launch, CancellationToken cancellationToken);

    Task<Launch?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Launch>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyList<Launch>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);
}
