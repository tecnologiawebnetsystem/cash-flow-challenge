using System.Net;
using System.Net.Http.Json;
using CashFlow.Api.Contracts;
using CashFlow.Application.DTOs;
using CashFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CashFlow.IntegrationTests.Api;

[Collection(ApiCollectionFixture.Name)]
public class DailyBalancesControllerTests
{
    private readonly HttpClient _client;

    public DailyBalancesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetByDate_ShouldReturnNotFound_WhenDateHasNeverBeenConsolidated()
    {
        var date = UniqueDate();

        var response = await _client.GetAsync($"/api/v1/daily-balances/{date:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConsolidateThenGet_ShouldReturnClosingBalance_ReflectingAllLaunchesForTheDate()
    {
        var date = UniqueDate();

        await _client.PostAsJsonAsync("/api/v1/launches",
            new RegisterLaunchRequest("Venda", 500m, LaunchType.Credit, date));
        await _client.PostAsJsonAsync("/api/v1/launches",
            new RegisterLaunchRequest("Pagamento de fornecedor", 200m, LaunchType.Debit, date));

        // The consolidation endpoint is invoked explicitly here rather than
        // relying on the fire-and-forget background signal, so the test's
        // outcome does not depend on background worker timing.
        var consolidateResponse = await _client.PostAsync($"/api/v1/daily-balances/{date:yyyy-MM-dd}/consolidate", content: null);
        consolidateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var balance = await consolidateResponse.Content.ReadFromJsonAsync<DailyBalanceDto>();
        balance.Should().NotBeNull();
        balance!.TotalCredits.Should().Be(500m);
        balance.TotalDebits.Should().Be(200m);
        balance.ClosingBalance.Should().Be(300m);
        balance.Status.Should().Be(ConsolidationStatus.Consolidated);

        var getResponse = await _client.GetAsync($"/api/v1/daily-balances/{date:yyyy-MM-dd}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<DailyBalanceDto>();
        fetched.Should().NotBeNull();
        fetched!.ClosingBalance.Should().Be(300m);
    }

    [Fact]
    public async Task Consolidate_ShouldBeIdempotent_WhenCalledTwiceForTheSameDate()
    {
        var date = UniqueDate();

        await _client.PostAsJsonAsync("/api/v1/launches",
            new RegisterLaunchRequest("Venda única", 1000m, LaunchType.Credit, date));

        var first = await _client.PostAsync($"/api/v1/daily-balances/{date:yyyy-MM-dd}/consolidate", content: null);
        var second = await _client.PostAsync($"/api/v1/daily-balances/{date:yyyy-MM-dd}/consolidate", content: null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondBody = await second.Content.ReadFromJsonAsync<DailyBalanceDto>();
        secondBody!.ClosingBalance.Should().Be(1000m);

        // Idempotency is verified by checking that re-consolidating never
        // creates a duplicate row for the same date - the range query for
        // that single day must still return exactly one entry.
        var rangeResponse = await _client.GetAsync(
            $"/api/v1/daily-balances?startDate={date:yyyy-MM-dd}&endDate={date:yyyy-MM-dd}");
        var range = await rangeResponse.Content.ReadFromJsonAsync<List<DailyBalanceDto>>();
        range.Should().ContainSingle();
    }

    [Fact]
    public async Task GetRange_ShouldReturnConsolidatedBalances_OrderedByDate()
    {
        var startDate = UniqueDate();
        var endDate = startDate.AddDays(1);

        await _client.PostAsJsonAsync("/api/v1/launches",
            new RegisterLaunchRequest("Dia 1", 100m, LaunchType.Credit, startDate));
        await _client.PostAsJsonAsync("/api/v1/launches",
            new RegisterLaunchRequest("Dia 2", 50m, LaunchType.Credit, endDate));

        await _client.PostAsync($"/api/v1/daily-balances/{startDate:yyyy-MM-dd}/consolidate", content: null);
        await _client.PostAsync($"/api/v1/daily-balances/{endDate:yyyy-MM-dd}/consolidate", content: null);

        var response = await _client.GetAsync(
            $"/api/v1/daily-balances?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var balances = await response.Content.ReadFromJsonAsync<List<DailyBalanceDto>>();
        balances.Should().NotBeNull();
        balances!.Should().HaveCount(2);
        balances.Select(b => b.ReferenceDate).Should().BeInAscendingOrder();
    }

    // A wide, distinct historical range so this suite never collides with
    // dates used by LaunchesControllerTests sharing the same container.
    private static DateOnly UniqueDate() =>
        new(2010, 1, 1).AddDays(Random.Shared.Next(1, 3650));
}
