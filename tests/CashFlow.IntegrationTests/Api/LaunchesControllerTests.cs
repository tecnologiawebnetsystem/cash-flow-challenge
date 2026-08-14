using System.Net;
using System.Net.Http.Json;
using CashFlow.Api.Contracts;
using CashFlow.Application.DTOs;
using CashFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CashFlow.IntegrationTests.Api;

[Collection(ApiCollectionFixture.Name)]
public class LaunchesControllerTests
{
    private readonly HttpClient _client;

    public LaunchesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostLaunch_ShouldReturnCreated_WithLocationHeader_WhenPayloadIsValid()
    {
        // Each test uses a distinct, deterministic-but-unique date so that
        // sequential runs against the shared container never collide on
        // the same aggregate.
        var launchDate = UniqueDate();
        var request = new RegisterLaunchRequest("Venda de mercadoria", 150.75m, LaunchType.Credit, launchDate);

        var response = await _client.PostAsJsonAsync("/api/v1/launches", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<LaunchDto>();
        body.Should().NotBeNull();
        body!.Amount.Should().Be(150.75m);
        body.Type.Should().Be(LaunchType.Credit);
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("Descrição válida", 0)]
    [InlineData("Descrição válida", -10)]
    public async Task PostLaunch_ShouldReturnBadRequest_WhenPayloadIsInvalid(string description, decimal amount)
    {
        var request = new RegisterLaunchRequest(description, amount, LaunchType.Credit, UniqueDate());

        var response = await _client.PostAsJsonAsync("/api/v1/launches", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostLaunch_ShouldReturnBadRequest_WhenLaunchDateIsInTheFuture()
    {
        var request = new RegisterLaunchRequest(
            "Lançamento futuro", 10m, LaunchType.Credit, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5));

        var response = await _client.PostAsJsonAsync("/api/v1/launches", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetLaunchesByDate_ShouldReturnOnlyLaunchesForThatDate()
    {
        var launchDate = UniqueDate();
        var otherDate = launchDate.AddDays(1);

        await _client.PostAsJsonAsync("/api/v1/launches",
            new RegisterLaunchRequest("Lançamento do dia", 100m, LaunchType.Credit, launchDate));
        await _client.PostAsJsonAsync("/api/v1/launches",
            new RegisterLaunchRequest("Lançamento de outro dia", 999m, LaunchType.Debit, otherDate));

        var response = await _client.GetAsync($"/api/v1/launches?date={launchDate:yyyy-MM-dd}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var launches = await response.Content.ReadFromJsonAsync<List<LaunchDto>>();
        launches.Should().NotBeNull();
        launches!.Should().ContainSingle(l => l.Description == "Lançamento do dia");
        launches.Should().NotContain(l => l.Description == "Lançamento de outro dia");
    }

    // Spread across a wide historical range (rather than "today") so this
    // suite never collides with dates used by the consolidation tests,
    // which register launches for "today" and "yesterday".
    private static DateOnly UniqueDate() =>
        new(2000, 1, 1).AddDays(Random.Shared.Next(1, 7300));
}
