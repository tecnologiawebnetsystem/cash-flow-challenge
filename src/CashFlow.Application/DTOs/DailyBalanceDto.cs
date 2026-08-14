using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;

namespace CashFlow.Application.DTOs;

/// <summary>
/// Projeção de leitura de <see cref="DailyBalance"/> exposta pela camada de
/// Application. Mantém a Api desacoplada dos detalhes internos da entidade
/// de domínio (ex.: <see cref="DailyBalance.RowVersion"/>).
/// </summary>
public sealed record DailyBalanceDto(
    DateOnly ReferenceDate,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal ClosingBalance,
    ConsolidationStatus Status,
    DateTime? ConsolidatedAtUtc,
    string? FailureReason)
{
    public static DailyBalanceDto FromDomain(DailyBalance dailyBalance) => new(
        dailyBalance.ReferenceDate,
        dailyBalance.TotalCredits,
        dailyBalance.TotalDebits,
        dailyBalance.ClosingBalance,
        dailyBalance.Status,
        dailyBalance.ConsolidatedAtUtc,
        dailyBalance.FailureReason);
}
