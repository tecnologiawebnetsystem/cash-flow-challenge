namespace CashFlow.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the persistence transaction boundary. Application
/// handlers commit through this interface without knowing whether the
/// implementation is EF Core, Dapper, or anything else (DIP).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
