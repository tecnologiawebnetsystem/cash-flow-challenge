using CashFlow.Application.Consolidation.Commands.ConsolidateDailyBalance;
using CashFlow.Application.Consolidation.Queries.GetDailyBalance;
using CashFlow.Application.Consolidation.Queries.GetDailyBalanceRange;
using CashFlow.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Api.Controllers;

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


    [HttpGet("{date}")]
    [ProducesResponseType(typeof(DailyBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DailyBalanceDto>> GetByDate(DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDailyBalanceQuery(date), cancellationToken);
        return Ok(result);
    }

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

    [HttpPost("{date}/consolidate")]
    [ProducesResponseType(typeof(DailyBalanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyBalanceDto>> Consolidate(DateOnly date, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ConsolidateDailyBalanceCommand(date), cancellationToken);
        return Ok(result);
    }
}
