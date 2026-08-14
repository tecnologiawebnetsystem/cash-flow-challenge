using FluentValidation;

namespace CashFlow.Application.Launches.Commands.RegisterLaunch;

public sealed class RegisterLaunchCommandValidator : AbstractValidator<RegisterLaunchCommand>
{
    public RegisterLaunchCommandValidator()
    {
        RuleFor(command => command.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(200).WithMessage("A descrição deve ter no máximo 200 caracteres.");

        RuleFor(command => command.Amount)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(command => command.Type)
            .IsInEnum().WithMessage("O tipo deve ser Credit (entrada) ou Debit (despesa).");

        RuleFor(command => command.LaunchDate)
            .Must(date => date is null || date.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("A data do lançamento não pode ser no futuro.");
    }
}
