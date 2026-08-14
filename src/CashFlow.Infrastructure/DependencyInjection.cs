using CashFlow.Application.Common.Interfaces;
using CashFlow.Domain.Repositories;
using CashFlow.Infrastructure.BackgroundServices;
using CashFlow.Infrastructure.Messaging;
using CashFlow.Infrastructure.Persistence;
using CashFlow.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CashFlow.Infrastructure;

/// <summary>
/// Composition root for every Infrastructure concern: persistence,
/// messaging, background processing and the system clock. The Api project
/// only calls <see cref="AddInfrastructure"/> - it never references EF
/// Core, Npgsql or Polly directly.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CashFlowDatabase")
            ?? throw new InvalidOperationException("Connection string 'CashFlowDatabase' is not configured.");

        services.AddDbContext<CashFlowDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<ILaunchRepository, LaunchRepository>();
        services.AddScoped<IDailyBalanceRepository, DailyBalanceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IConsolidationFailureRecorder, ConsolidationFailureRecorder>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Single shared queue instance: producers (API requests) and the
        // single consumer (ConsolidationWorker) must see the same channel.
        services.AddSingleton<InMemoryConsolidationQueue>();
        services.AddSingleton<IConsolidationQueue>(sp => sp.GetRequiredService<InMemoryConsolidationQueue>());

        services.AddHostedService<ConsolidationWorker>();
        services.AddHostedService<ReconciliationWorker>();

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql");

        return services;
    }
}
