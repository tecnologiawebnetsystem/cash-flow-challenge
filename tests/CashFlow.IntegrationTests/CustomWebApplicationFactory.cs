using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace CashFlow.IntegrationTests;

/// <summary>
/// Inicializa o pipeline real da Api contra uma instância descartável e
/// containerizada do PostgreSQL. Usar um banco de dados real (em vez do
/// provider InMemory do EF Core) é intencional: esta suíte exercita o
/// provider Npgsql real, as restrições de unicidade e os tokens de
/// concorrência configurados em CashFlow.Infrastructure, que o provider
/// InMemory ignoraria silenciosamente.
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

        // Nenhuma outra sobrescrita é necessária: o Program.cs já aplica as
        // migrations pendentes do EF Core contra a connection string
        // resolvida na inicialização, que agora aponta para o container de teste.
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
