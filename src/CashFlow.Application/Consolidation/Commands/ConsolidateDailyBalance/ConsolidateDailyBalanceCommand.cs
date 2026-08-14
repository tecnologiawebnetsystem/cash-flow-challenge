using CashFlow.Application.DTOs;
using MediatR;

namespace CashFlow.Application.Consolidation.Commands.ConsolidateDailyBalance;

/// <summary>
/// Recomputes the consolidated balance for a single date from scratch,
/// based on every launch registered for that date. Fully idempotent:
/// running it twice for the same date yields the same result, which is
/// what makes it safe to retry after a transient failure.
/// </summary>
public sealed record ConsolidateDailyBalanceCommand(DateOnly Date) : IRequest<DailyBalanceDto>;
