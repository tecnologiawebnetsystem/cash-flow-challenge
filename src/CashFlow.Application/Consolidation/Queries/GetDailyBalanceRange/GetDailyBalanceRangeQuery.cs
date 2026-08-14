using CashFlow.Application.DTOs;
using MediatR;

namespace CashFlow.Application.Consolidation.Queries.GetDailyBalanceRange;

/// <summary>
/// Alimenta o "relatório de saldo diário consolidado": a lista de saldos
/// diários em um intervalo de datas, exigida pelo negócio.
/// </summary>
public sealed record GetDailyBalanceRangeQuery(DateOnly StartDate, DateOnly EndDate)
    : IRequest<IReadOnlyList<DailyBalanceDto>>;
