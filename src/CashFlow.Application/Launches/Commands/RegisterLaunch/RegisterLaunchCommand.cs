using CashFlow.Application.DTOs;
using CashFlow.Domain.Enums;
using MediatR;

namespace CashFlow.Application.Launches.Commands.RegisterLaunch;

/// <summary>
/// Registers a new credit or debit launch (lançamento) in the cash flow.
/// </summary>
public sealed record RegisterLaunchCommand(
    string Description,
    decimal Amount,
    LaunchType Type,
    DateOnly? LaunchDate) : IRequest<LaunchDto>;
