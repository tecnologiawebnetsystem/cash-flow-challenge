using CashFlow.Domain.Enums;

namespace CashFlow.Api.Contracts;

/// <summary>
/// Api-facing request shape for registering a launch. Kept separate from
/// <c>RegisterLaunchCommand</c> so the Application layer's contracts never
/// leak HTTP/transport concerns (e.g. model binding attributes).
/// </summary>
public sealed record RegisterLaunchRequest(
    string Description,
    decimal Amount,
    LaunchType Type,
    DateOnly? LaunchDate);
