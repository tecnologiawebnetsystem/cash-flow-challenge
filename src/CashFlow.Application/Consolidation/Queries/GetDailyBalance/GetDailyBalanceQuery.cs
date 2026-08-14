using CashFlow.Application.DTOs;
using MediatR;

namespace CashFlow.Application.Consolidation.Queries.GetDailyBalance;

public sealed record GetDailyBalanceQuery(DateOnly Date) : IRequest<DailyBalanceDto>;
