using FluentValidation;

namespace CashFlow.Application.Consolidation.Queries.GetDailyBalanceRange;

public sealed class GetDailyBalanceRangeQueryValidator : AbstractValidator<GetDailyBalanceRangeQuery>
{
    public GetDailyBalanceRangeQueryValidator()
    {
        RuleFor(query => query.EndDate)
            .GreaterThanOrEqualTo(query => query.StartDate)
            .WithMessage("EndDate must be greater than or equal to StartDate.");

        RuleFor(query => query)
            .Must(query => query.EndDate.DayNumber - query.StartDate.DayNumber <= 366)
            .WithMessage("The date range cannot span more than 366 days.");
    }
}
