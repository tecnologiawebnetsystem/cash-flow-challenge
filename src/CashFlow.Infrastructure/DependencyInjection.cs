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

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CashFlowDatabase")
            ?? throw new InvalidOperationException("A connection string 'CashFlowDatabase' não está configurada.");

        services.AddDbContext<CashFlowDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<ILaunchRepository, LaunchRepository>();
        services.AddScoped<IDailyBalanceRepository, DailyBalanceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IConsolidationFailureRecorder, ConsolidationFailureRecorder>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddSingleton<InMemoryConsolidationQueue>();
        services.AddSingleton<IConsolidationQueue>(sp => sp.GetRequiredService<InMemoryConsolidationQueue>());

        services.AddHostedService<ConsolidationWorker>();
        services.AddHostedService<ReconciliationWorker>();

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql");

        return services;
    }
}
