using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CashFlow.Infrastructure.Persistence;

/// <summary>
/// Allows `dotnet ef migrations add` / `dotnet ef database update` to run
/// from the CLI without needing to spin up the full Api host. Only used at
/// design time.
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
