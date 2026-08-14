using CashFlow.Application.DTOs;
using MediatR;

namespace CashFlow.Application.Consolidation.Queries.GetDailyBalanceRange;


public sealed record GetDailyBalanceRangeQuery(DateOnly StartDate, DateOnly EndDate)
    : IRequest<IReadOnlyList<DailyBalanceDto>>;
