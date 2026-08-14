using System.Net;
using System.Text.Json;
using CashFlow.Application.Common.Exceptions;
using CashFlow.Domain.Exceptions;

namespace CashFlow.Api.Middleware;

/// <summary>
/// Ponto único onde toda exceção não tratada é traduzida em uma resposta
/// consistente no formato problem+json. Mantém as preocupações de
/// mapeamento de erro totalmente fora dos controllers (Princípio da
/// Responsabilidade Única).
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
            _logger.LogError(exception, "Exceção não tratada ao processar {Path}.", context.Request.Path);
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
        ValidationException => (HttpStatusCode.BadRequest, "Ocorreram um ou mais erros de validação."),
        NotFoundException => (HttpStatusCode.NotFound, "Recurso não encontrado."),
        DomainException => (HttpStatusCode.UnprocessableEntity, "Violação de regra de negócio."),
        _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro inesperado."),
    };
}
