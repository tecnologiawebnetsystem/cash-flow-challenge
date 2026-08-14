using Xunit;

namespace CashFlow.IntegrationTests;

/// <summary>
/// Shares a single containerized PostgreSQL + WebApplicationFactory across
/// every test class in the "Api" collection, so the (relatively expensive)
/// container startup happens once per test run instead of once per class.
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollectionFixture : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Api";
}
