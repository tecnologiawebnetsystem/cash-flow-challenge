using CashFlow.Api.Contracts;
using CashFlow.Application.DTOs;
using CashFlow.Application.Launches.Commands.RegisterLaunch;
using CashFlow.Application.Launches.Queries.GetLaunchesByDate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Api.Controllers;

/// <summary>
/// Write/read surface for individual cash flow launches (lançamentos).
/// This controller is intentionally thin: it only translates HTTP
/// concerns into MediatR requests. All business logic lives in the
/// Application layer handlers.
/// </summary>
[ApiController]
[Route("api/v1/launches")]
[Produces("application/json")]
public sealed class LaunchesController : ControllerBase
{
    private readonly ISender _sender;

    public LaunchesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Registers a new credit or debit launch.</summary>
    /// <response code="201">The launch was registered successfully.</response>
    /// <response code="400">The request failed validation.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LaunchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LaunchDto>> Register(
        [FromBody] RegisterLaunchRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterLaunchCommand(request.Description, request.Amount, request.Type, request.LaunchDate);
        var result = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetByDate), new { date = result.LaunchDate }, result);
    }

    /// <summary>Lists every launch registered for a given date.</summary>
    /// <response code="200">The list of launches for the date (possibly empty).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LaunchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LaunchDto>>> GetByDate(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLaunchesByDateQuery(date), cancellationToken);
        return Ok(result);
    }
}
