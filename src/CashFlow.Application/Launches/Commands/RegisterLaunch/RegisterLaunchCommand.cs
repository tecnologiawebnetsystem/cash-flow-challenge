using CashFlow.Application.DTOs;
using CashFlow.Domain.Enums;
using MediatR;

namespace CashFlow.Application.Launches.Commands.RegisterLaunch;

public sealed record RegisterLaunchCommand(
    string Description,
    decimal Amount,
    LaunchType Type,
    DateOnly? LaunchDate) : IRequest<LaunchDto>;
