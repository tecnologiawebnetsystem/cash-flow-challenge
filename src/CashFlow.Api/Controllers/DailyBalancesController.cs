using CashFlow.Application.Consolidation.Commands.ConsolidateDailyBalance;
using CashFlow.Application.Consolidation.Queries.GetDailyBalance;
using CashFlow.Application.Consolidation.Queries.GetDailyBalanceRange;
using CashFlow.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Api.Controllers;

/// <summary>
/// Read surface for the consolidated daily balance report, plus a manual
/// trigger endpoint useful for demos and operational recovery.
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

    /// <summary>Gets the consolidated balance for a single date.</summary>
    /// <response code="200">The consolidated balance for the date.</response>
    /// <response code="404">No balance has been consolidated yet for that date.</response>
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(DailyBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DailyBalanceDto>> GetByDate(DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDailyBalanceQuery(date), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the consolidated daily balance report for a date range
    /// (the "relatório de saldo diário consolidado" required by the business).
    /// </summary>
    /// <response code="200">The list of consolidated balances in the range.</response>
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
    /// Forces a synchronous (re)consolidation of a given date. Intended for
    /// demos and manual operational recovery - the normal flow consolidates
    /// automatically and asynchronously whenever a launch is registered.
    /// </summary>
    /// <response code="200">The freshly (re)consolidated balance.</response>
    [HttpPost("{date}/consolidate")]
    [ProducesResponseType(typeof(DailyBalanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyBalanceDto>> Consolidate(DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ConsolidateDailyBalanceCommand(date), cancellationToken);
        return Ok(result);
    }
}
