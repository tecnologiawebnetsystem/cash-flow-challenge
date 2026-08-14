using System.Net;
using System.Text.Json;
using CashFlow.Application.Common.Exceptions;
using CashFlow.Domain.Exceptions;

namespace CashFlow.Api.Middleware;

/// <summary>
/// Single place where every unhandled exception is translated into a
/// consistent problem+json-shaped response. Keeps error-mapping concerns
/// out of controllers entirely (Single Responsibility Principle).
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = MapException(exception);

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Path}.", context.Request.Path);
        }
        else
        {
            _logger.LogWarning("{Title}: {Message}", title, exception.Message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problem = new
        {
            type = $"https://httpstatuses.io/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail = exception.Message,
            errors = exception is ValidationException validationException ? validationException.Errors : null,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        }));
    }

    private static (HttpStatusCode StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        ValidationException => (HttpStatusCode.BadRequest, "One or more validation errors occurred."),
        NotFoundException => (HttpStatusCode.NotFound, "Resource not found."),
        DomainException => (HttpStatusCode.UnprocessableEntity, "Business rule violation."),
        _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred."),
    };
}
