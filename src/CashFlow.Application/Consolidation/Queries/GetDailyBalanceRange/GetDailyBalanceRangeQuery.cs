using CashFlow.Application.DTOs;
using MediatR;

namespace CashFlow.Application.Consolidation.Queries.GetDailyBalanceRange;

/// <summary>
/// Powers the "relatório de saldo diário consolidado": the list of daily
/// balances for a date range, requested by the business.
/// </summary>
public sealed record GetDailyBalanceRangeQuery(DateOnly StartDate, DateOnly EndDate)
    : IRequest<IReadOnlyList<DailyBalanceDto>>;
