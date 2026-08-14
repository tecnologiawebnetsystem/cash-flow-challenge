using Xunit;

namespace CashFlow.IntegrationTests;

/// <summary>
/// Compartilha uma única instância containerizada de PostgreSQL +
/// WebApplicationFactory entre todas as classes de teste da coleção "Api",
/// para que a inicialização do container (relativamente custosa) ocorra
/// uma única vez por execução de testes, em vez de uma vez por classe.
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollectionFixture : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Api";
}
