using CashFlow.Application.Consolidation.Commands.ConsolidateDailyBalance;
using CashFlow.Application.Consolidation.Queries.GetDailyBalance;
using CashFlow.Application.Consolidation.Queries.GetDailyBalanceRange;
using CashFlow.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Api.Controllers;

/// <summary>
/// Superfície de leitura do relatório de saldo diário consolidado, além de
/// um endpoint de disparo manual útil para demonstrações e recuperação
/// operacional.
/// </summary>
[ApiController]
[Route("api/v1/daily-balances")]
[Produces("application/json")]
public sealed class DailyBalancesController : ControllerBase
{
    private readonly ISender _sender;

    public DailyBalancesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Obtém o saldo consolidado de uma única data.</summary>
    /// <response code="200">O saldo consolidado da data informada.</response>
    /// <response code="404">Ainda não há saldo consolidado para essa data.</response>
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(DailyBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DailyBalanceDto>> GetByDate(DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDailyBalanceQuery(date), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtém o relatório de saldo diário consolidado para um intervalo de
    /// datas (o "relatório de saldo diário consolidado" exigido pelo negócio).
    /// </summary>
    /// <response code="200">A lista de saldos consolidados no intervalo.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DailyBalanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DailyBalanceDto>>> GetRange(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDailyBalanceRangeQuery(startDate, endDate), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Força a (re)consolidação síncrona de uma data específica. Pensado
    /// para demonstrações e recuperação operacional manual - o fluxo normal
    /// consolida automaticamente e de forma assíncrona sempre que um
    /// lançamento é registrado.
    /// </summary>
    /// <response code="200">O saldo recém (re)consolidado.</response>
    [HttpPost("{date}/consolidate")]
    [ProducesResponseType(typeof(DailyBalanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyBalanceDto>> Consolidate(DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ConsolidateDailyBalanceCommand(date), cancellationToken);
        return Ok(result);
    }
}
