using CashFlow.Api.Contracts;
using CashFlow.Application.DTOs;
using CashFlow.Application.Launches.Commands.RegisterLaunch;
using CashFlow.Application.Launches.Queries.GetLaunchesByDate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Api.Controllers;

/// <summary>
/// Superfície de escrita/leitura para lançamentos individuais de fluxo de
/// caixa. Este controller é propositalmente enxuto: ele apenas traduz
/// preocupações de HTTP em requisições MediatR. Toda a lógica de negócio
/// vive nos handlers da camada de Application.
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

    /// <summary>Registra um novo lançamento de crédito ou débito.</summary>
    /// <response code="201">O lançamento foi registrado com sucesso.</response>
    /// <response code="400">A requisição falhou na validação.</response>
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

    /// <summary>Lista todos os lançamentos registrados em uma determinada data.</summary>
    /// <response code="200">A lista de lançamentos da data (pode ser vazia).</response>
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
