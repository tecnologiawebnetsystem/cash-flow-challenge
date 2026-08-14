using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Events;
using CashFlow.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace CashFlow.UnitTests.Domain;

public class LaunchTests
{
    private static readonly DateOnly SomeDate = new(2026, 8, 14);
    private static readonly DateTime SomeInstant = new(2026, 8, 14, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldSucceed_WithValidCreditData()
    {
        var launch = Launch.Create("Venda de produto", 150.75m, LaunchType.Credit, SomeDate, SomeInstant);

        launch.Description.Should().Be("Venda de produto");
        launch.Amount.Amount.Should().Be(150.75m);
        launch.Type.Should().Be(LaunchType.Credit);
        launch.LaunchDate.Should().Be(SomeDate);
        launch.CreatedAtUtc.Should().Be(SomeInstant);
    }

    [Fact]
    public void Create_ShouldTrimDescription()
    {
        var launch = Launch.Create("   Pagamento fornecedor   ", 50m, LaunchType.Debit, SomeDate, SomeInstant);

        launch.Description.Should().Be("Pagamento fornecedor");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrow_WhenDescriptionIsMissing(string? description)
    {
        var act = () => Launch.Create(description!, 10m, LaunchType.Credit, SomeDate, SomeInstant);

        act.Should().Throw<InvalidLaunchDescriptionException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenDescriptionExceedsMaxLength()
    {
        var tooLong = new string('a', 201);

        var act = () => Launch.Create(tooLong, 10m, LaunchType.Credit, SomeDate, SomeInstant);

        act.Should().Throw<InvalidLaunchDescriptionException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_ShouldThrow_WhenAmountIsNotPositive(decimal amount)
    {
        var act = () => Launch.Create("Descrição válida", amount, LaunchType.Credit, SomeDate, SomeInstant);

        act.Should().Throw<InvalidLaunchAmountException>();
    }

    [Fact]
    public void Create_ShouldRaiseLaunchRegisteredEvent()
    {
        var launch = Launch.Create("Venda", 100m, LaunchType.Credit, SomeDate, SomeInstant);

        launch.DomainEvents.Should().ContainSingle();
        launch.DomainEvents.Single().Should().BeOfType<LaunchRegisteredEvent>();
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyTheCollection()
    {
        var launch = Launch.Create("Venda", 100m, LaunchType.Credit, SomeDate, SomeInstant);

        launch.ClearDomainEvents();

        launch.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SignedAmount_ShouldBePositive_ForCredit()
    {
        var launch = Launch.Create("Venda", 100m, LaunchType.Credit, SomeDate, SomeInstant);

        launch.SignedAmount.Should().Be(100m);
    }

    [Fact]
    public void SignedAmount_ShouldBeNegative_ForDebit()
    {
        var launch = Launch.Create("Compra", 100m, LaunchType.Debit, SomeDate, SomeInstant);

        launch.SignedAmount.Should().Be(-100m);
    }
}
