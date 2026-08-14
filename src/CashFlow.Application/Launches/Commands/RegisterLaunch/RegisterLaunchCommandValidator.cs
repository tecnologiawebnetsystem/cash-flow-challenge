using FluentValidation;

namespace CashFlow.Application.Launches.Commands.RegisterLaunch;

public sealed class RegisterLaunchCommandValidator : AbstractValidator<RegisterLaunchCommand>
{
    public RegisterLaunchCommandValidator()
    {
        RuleFor(command => command.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(200).WithMessage("Description must be at most 200 characters long.");

        RuleFor(command => command.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(command => command.Type)
            .IsInEnum().WithMessage("Type must be either Credit or Debit.");

        RuleFor(command => command.LaunchDate)
            .Must(date => date is null || date.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Launch date cannot be in the future.");
    }
}
