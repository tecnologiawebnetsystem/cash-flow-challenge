using CashFlow.Application.DTOs;
using CashFlow.Domain.Repositories;
using MediatR;

namespace CashFlow.Application.Launches.Queries.GetLaunchesByDate;

public sealed class GetLaunchesByDateQueryHandler
    : IRequestHandler<GetLaunchesByDateQuery, IReadOnlyList<LaunchDto>>
{
    private readonly ILaunchRepository _launchRepository;

    public GetLaunchesByDateQueryHandler(ILaunchRepository launchRepository)
    {
        _launchRepository = launchRepository;
    }

    public async Task<IReadOnlyList<LaunchDto>> Handle(GetLaunchesByDateQuery request, CancellationToken cancellationToken)
    {
        var launches = await _launchRepository.GetByDateAsync(request.Date, cancellationToken);
        return launches.Select(LaunchDto.FromDomain).ToList();
    }
}
