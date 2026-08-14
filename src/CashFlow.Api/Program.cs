using System.Text.Json.Serialization;
using CashFlow.Api.Middleware;
using CashFlow.Application;
using CashFlow.Infrastructure;
using CashFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    // Enums are exposed as their string names (e.g. "Credit"/"Debit")
    // instead of raw numeric values, which is both more readable in the
    // Swagger UI and less brittle for API consumers.
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cash Flow API",
        Version = "v1",
        Description = "Gestão de lançamentos financeiros e saldo diário consolidado.",
    });
});

var app = builder.Build();

// Baseline security headers (defense-in-depth). This API has no browser UI,
// so framing/CSP are irrelevant, but the transport/content-type hardening
// still applies to every response.
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health");

// Applies pending EF Core migrations automatically on startup. Acceptable
// for this challenge's scope; a production rollout would run migrations
// as a separate release step instead.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CashFlowDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
