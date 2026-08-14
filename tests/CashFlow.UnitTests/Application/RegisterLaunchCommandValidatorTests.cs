using CashFlow.Application.Launches.Commands.RegisterLaunch;
using CashFlow.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CashFlow.UnitTests.Application;

public class RegisterLaunchCommandValidatorTests
{
    private readonly RegisterLaunchCommandValidator _validator = new();

    [Fact]
    public void ShouldNotHaveErrors_WhenCommandIsValid()
    {
        var command = new RegisterLaunchCommand("Venda de produto", 100m, LaunchType.Credit, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldHaveError_WhenDescriptionIsEmpty(string description)
    {
        var command = new RegisterLaunchCommand(description, 100m, LaunchType.Credit, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void ShouldHaveError_WhenDescriptionExceedsMaxLength()
    {
        var command = new RegisterLaunchCommand(new string('x', 201), 100m, LaunchType.Credit, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldHaveError_WhenAmountIsNotPositive(decimal amount)
    {
        var command = new RegisterLaunchCommand("Descrição", amount, LaunchType.Credit, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Amount);
    }

    [Fact]
    public void ShouldHaveError_WhenLaunchDateIsInTheFuture()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var command = new RegisterLaunchCommand("Descrição", 10m, LaunchType.Credit, futureDate);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.LaunchDate);
    }

    [Fact]
    public void ShouldNotHaveError_WhenLaunchDateIsToday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var command = new RegisterLaunchCommand("Descrição", 10m, LaunchType.Credit, today);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.LaunchDate);
    }
}
