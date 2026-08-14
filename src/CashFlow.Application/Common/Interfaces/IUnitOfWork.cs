namespace CashFlow.Application.Common.Interfaces;

/// <summary>
/// Abstração sobre o limite da transação de persistência. Os handlers da
/// Application confirmam (commit) através desta interface sem saber se a
/// implementação é EF Core, Dapper ou qualquer outra tecnologia (DIP).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
