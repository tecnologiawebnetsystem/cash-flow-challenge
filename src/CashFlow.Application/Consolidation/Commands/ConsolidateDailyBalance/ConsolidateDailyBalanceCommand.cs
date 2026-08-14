using CashFlow.Application.DTOs;
using MediatR;

namespace CashFlow.Application.Consolidation.Commands.ConsolidateDailyBalance;

/// <summary>
/// Recalcula do zero o saldo consolidado de uma única data, com base em
/// todos os lançamentos registrados nessa data. Totalmente idempotente:
/// executá-lo duas vezes para a mesma data produz o mesmo resultado, o que
/// é o que permite reexecutá-lo com segurança após uma falha transitória.
/// </summary>
public sealed record ConsolidateDailyBalanceCommand(DateOnly Date) : IRequest<DailyBalanceDto>;
