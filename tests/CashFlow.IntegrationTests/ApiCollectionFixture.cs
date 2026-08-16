using Xunit;

namespace CashFlow.IntegrationTests;

[CollectionDefinition(Name)]
public class ApiCollectionFixture : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Api";
}
