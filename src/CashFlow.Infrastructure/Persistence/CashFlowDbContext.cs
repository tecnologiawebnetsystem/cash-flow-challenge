using CashFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Persistence;

/// <summary>
/// Contexto de persistência do EF Core. Conhece apenas os agregados do domínio,
/// nunca é referenciado diretamente pela camada de Application (apenas via interfaces).
/// </summary>
public sealed class CashFlowDbContext : DbContext
{
    public CashFlowDbContext(DbContextOptions<CashFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Launch> Launches => Set<Launch>();

    public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CashFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
