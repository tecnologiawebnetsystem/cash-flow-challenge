using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CashFlow.UnitTests.Domain;

public class DailyBalanceTests
{
    private static readonly DateOnly SomeDate = new(2026, 8, 14);
    private static readonly DateTime SomeInstant = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreatePending_ShouldStartWithZeroedTotalsAndPendingStatus()
    {
        var balance = DailyBalance.CreatePending(SomeDate);

        balance.ReferenceDate.Should().Be(SomeDate);
        balance.TotalCredits.Should().Be(0m);
        balance.TotalDebits.Should().Be(0m);
        balance.ClosingBalance.Should().Be(0m);
        balance.Status.Should().Be(ConsolidationStatus.Pending);
        balance.ConsolidatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Consolidate_ShouldComputeClosingBalanceAsCreditsMinusDebits()
    {
        var balance = DailyBalance.CreatePending(SomeDate);

        balance.Consolidate(totalCredits: 500m, totalDebits: 300m, SomeInstant);

        balance.TotalCredits.Should().Be(500m);
        balance.TotalDebits.Should().Be(300m);
        balance.ClosingBalance.Should().Be(200m);
        balance.Status.Should().Be(ConsolidationStatus.Consolidated);
        balance.ConsolidatedAtUtc.Should().Be(SomeInstant);
    }

    [Fact]
    public void Consolidate_ShouldAllowNegativeClosingBalance_WhenDebitsExceedCredits()
    {
        var balance = DailyBalance.CreatePending(SomeDate);

        balance.Consolidate(totalCredits: 100m, totalDebits: 400m, SomeInstant);

        balance.ClosingBalance.Should().Be(-300m);
    }

    [Fact]
    public void Consolidate_ShouldBeIdempotent_WhenCalledMultipleTimesWithSameInputs()
    {
        var balance = DailyBalance.CreatePending(SomeDate);

        balance.Consolidate(200m, 50m, SomeInstant);
        var firstResult = balance.ClosingBalance;

        balance.Consolidate(200m, 50m, SomeInstant.AddMinutes(5));
        var secondResult = balance.ClosingBalance;

        secondResult.Should().Be(firstResult);
    }

    [Fact]
    public void Consolidate_ShouldClearPreviousFailureState()
    {
        var balance = DailyBalance.CreatePending(SomeDate);
        balance.MarkAsFailed("Database timeout");

        balance.Consolidate(100m, 0m, SomeInstant);

        balance.Status.Should().Be(ConsolidationStatus.Consolidated);
        balance.FailureReason.Should().BeNull();
        balance.FailedAttempts.Should().Be(0);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetStatusAndIncrementAttempts()
    {
        var balance = DailyBalance.CreatePending(SomeDate);

        balance.MarkAsFailed("Connection refused");
        balance.MarkAsFailed("Connection refused");

        balance.Status.Should().Be(ConsolidationStatus.Failed);
        balance.FailedAttempts.Should().Be(2);
        balance.FailureReason.Should().Be("Connection refused");
    }
}
