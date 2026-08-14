using System.Reflection;
using CashFlow.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Application;

/// <summary>
/// Ponto único de composição da camada de Application.
/// Mantém a camada de Api livre de conhecimento sobre MediatR/FluentValidation,
/// respeitando o princípio da Inversão de Dependência (SOLID - D).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline (Decorator/Chain of Responsibility) executado em toda requisição:
            // 1) Log de entrada/saída  2) Validação automática via FluentValidation
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
