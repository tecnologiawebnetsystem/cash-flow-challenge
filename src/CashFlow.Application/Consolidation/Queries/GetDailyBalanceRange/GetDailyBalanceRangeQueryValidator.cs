using FluentValidation;

namespace CashFlow.Application.Consolidation.Queries.GetDailyBalanceRange;

public sealed class GetDailyBalanceRangeQueryValidator : AbstractValidator<GetDailyBalanceRangeQuery>
{
    public GetDailyBalanceRangeQueryValidator()
    {
        RuleFor(query => query.EndDate)
            .GreaterThanOrEqualTo(query => query.StartDate)
            .WithMessage("A data final deve ser maior ou igual à data inicial.");

        RuleFor(query => query)
            .Must(query => query.EndDate.DayNumber - query.StartDate.DayNumber <= 366)
            .WithMessage("O intervalo de datas não pode ser maior que 366 dias.");
    }
}
