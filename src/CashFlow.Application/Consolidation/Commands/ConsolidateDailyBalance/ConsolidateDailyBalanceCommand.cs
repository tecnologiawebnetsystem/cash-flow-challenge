using CashFlow.Application.DTOs;
using MediatR;

namespace CashFlow.Application.Consolidation.Commands.ConsolidateDailyBalance;


public sealed record ConsolidateDailyBalanceCommand(DateOnly Date) : IRequest<DailyBalanceDto>;
