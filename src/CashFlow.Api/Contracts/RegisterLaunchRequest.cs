using CashFlow.Domain.Enums;

namespace CashFlow.Api.Contracts;

/// <summary>
/// Formato de requisição exposto pela Api para registrar um lançamento.
/// Mantido separado do <c>RegisterLaunchCommand</c> para que os contratos
/// da camada de Application nunca vazem preocupações de HTTP/transporte
/// (ex.: atributos de model binding).
/// </summary>
public sealed record RegisterLaunchRequest(
    string Description,
    decimal Amount,
    LaunchType Type,
    DateOnly? LaunchDate);
