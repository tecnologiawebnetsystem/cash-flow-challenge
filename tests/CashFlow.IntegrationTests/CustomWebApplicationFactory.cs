using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace CashFlow.IntegrationTests;

/// <summary>
/// Boots the real Api pipeline against a disposable, containerized
/// PostgreSQL instance. Using a real database (instead of the EF Core
/// InMemory provider) is intentional: this suite exercises the actual
/// Npgsql provider, unique constraints, and concurrency tokens configured
/// in CashFlow.Infrastructure, which the InMemory provider would silently
/// ignore.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("cashflow_tests")
        .WithUsername("cashflow")
        .WithPassword("cashflow")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CashFlowDatabase"] = _dbContainer.GetConnectionString(),
            });
        });

        // No further overrides needed: Program.cs already applies pending
        // EF Core migrations against whatever connection string is
        // resolved at startup, which now points at the test container.
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }
}
