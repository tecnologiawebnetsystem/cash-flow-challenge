using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CashFlow.Infrastructure.Persistence;

/// <summary>
/// Permite que `dotnet ef migrations add` / `dotnet ef database update`
/// sejam executados pela CLI sem precisar subir o host completo da Api.
/// Usado apenas em tempo de design.
/// </summary>
public sealed class CashFlowDbContextFactory : IDesignTimeDbContextFactory<CashFlowDbContext>
{
    public CashFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CASHFLOW_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=cashflow;Username=cashflow;Password=cashflow";

        var optionsBuilder = new DbContextOptionsBuilder<CashFlowDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new CashFlowDbContext(optionsBuilder.Options);
    }
}
