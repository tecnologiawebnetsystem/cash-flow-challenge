using CashFlow.Domain.Enums;

namespace CashFlow.Api.Contracts;

public sealed record RegisterLaunchRequest(
    string Description,
    decimal Amount,
    LaunchType Type,
    DateOnly? LaunchDate);
