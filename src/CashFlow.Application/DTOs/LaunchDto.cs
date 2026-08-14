using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;

namespace CashFlow.Application.DTOs;

public sealed record LaunchDto(
    Guid Id,
    string Description,
    decimal Amount,
    LaunchType Type,
    DateOnly LaunchDate,
    DateTime CreatedAtUtc)
{
    public static LaunchDto FromDomain(Launch launch) => new(
        launch.Id,
        launch.Description,
        launch.Amount.Amount,
        launch.Type,
        launch.LaunchDate,
        launch.CreatedAtUtc);
}
