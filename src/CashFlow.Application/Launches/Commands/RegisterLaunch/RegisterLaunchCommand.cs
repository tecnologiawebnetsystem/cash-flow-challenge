using CashFlow.Application.DTOs;
using CashFlow.Domain.Enums;
using MediatR;

namespace CashFlow.Application.Launches.Commands.RegisterLaunch;

/// <summary>
/// Registra um novo lançamento de crédito ou débito no fluxo de caixa.
/// </summary>
public sealed record RegisterLaunchCommand(
    string Description,
    decimal Amount,
    LaunchType Type,
    DateOnly? LaunchDate) : IRequest<LaunchDto>;
