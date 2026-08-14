using CashFlow.Domain.Entities;

namespace CashFlow.Domain.Repositories;

/// <summary>
/// Abstração de persistência para as projeções de <see cref="DailyBalance"/>.
/// </summary>
public interface IDailyBalanceRepository
{
    Task<DailyBalance?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlyList<DailyBalance>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken);

    /// <summary>
    /// Insere o saldo diário se ainda não existir um para a sua data, ou
    /// atualiza o existente caso contrário. A consolidação é idempotente
    /// por projeto.
    /// </summary>
    Task UpsertAsync(DailyBalance dailyBalance, CancellationToken cancellationToken);
}
