using CashFlow.Application.DTOs;
using MediatR;

namespace CashFlow.Application.Launches.Queries.GetLaunchesByDate;

public sealed record GetLaunchesByDateQuery(DateOnly Date) : IRequest<IReadOnlyList<LaunchDto>>;
