using CashFlow.Domain.Exceptions;
using CashFlow.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CashFlow.UnitTests.Domain;

public class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Create_ShouldThrow_WhenAmountIsZeroOrNegative(decimal amount)
    {
        var act = () => Money.Create(amount);

        act.Should().Throw<InvalidLaunchAmountException>();
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(10)]
    [InlineData(999999.99)]
    public void Create_ShouldSucceed_WhenAmountIsPositive(decimal amount)
    {
        var money = Money.Create(amount);

        money.Amount.Should().Be(amount);
    }

    [Fact]
    public void Create_ShouldRoundToTwoDecimalPlaces()
    {
        var money = Money.Create(10.126m);

        money.Amount.Should().Be(10.13m);
    }

    [Fact]
    public void Equality_ShouldBeBasedOnAmount()
    {
        var first = Money.Create(10m);
        var second = Money.Create(10m);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Equality_ShouldDistinguishDifferentAmounts()
    {
        var first = Money.Create(10m);
        var second = Money.Create(20m);

        first.Should().NotBe(second);
        (first == second).Should().BeFalse();
    }
}
