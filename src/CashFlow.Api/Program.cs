using System.Reflection;
using System.Text.Json.Serialization;
using CashFlow.Api.Middleware;
using CashFlow.Application;
using CashFlow.Infrastructure;
using CashFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    // Os enums são expostos pelo nome (ex.: "Credit"/"Debit") em vez do
    // valor numérico bruto, o que é mais legível no Swagger UI e menos
    // frágil para os consumidores da API.
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
        Description = "API de gestão de lançamentos financeiros (créditos e débitos) e do saldo diário " +
            "consolidado de um pequeno comércio. Consulte o README do repositório para o racional completo " +
            "de arquitetura, resiliência e estrutura de banco de dados.",
    });

    // Inclui os comentários /// dos controllers (resumo, parâmetros e
    // descrição de cada resposta HTTP) na UI do Swagger, em vez de expor
    // apenas as assinaturas dos endpoints sem contexto de negócio.
    var xmlFilePath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlFilePath))
    {
        options.IncludeXmlComments(xmlFilePath);
    }
});

var app = builder.Build();

// Cabeçalhos de segurança básicos (defesa em profundidade). Esta API não
// possui UI em navegador, então framing/CSP são irrelevantes, mas o
// hardening de transporte/content-type ainda se aplica a toda resposta.
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

// Aplica automaticamente as migrations pendentes do EF Core na inicialização.
// Aceitável para o escopo deste desafio; um deploy em produção executaria
// as migrations como uma etapa de release separada.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CashFlowDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();

// Exposto para os testes de integração baseados em WebApplicationFactory.
public partial class Program;
