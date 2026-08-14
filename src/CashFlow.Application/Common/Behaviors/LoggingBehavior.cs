using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CashFlow.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior (Decorator pattern) that logs the execution
/// and timing of every command/query, and surfaces unhandled exceptions
/// with full request context before they bubble up.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Handling {RequestName}", requestName);
            var response = await next();
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{RequestName} failed after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
