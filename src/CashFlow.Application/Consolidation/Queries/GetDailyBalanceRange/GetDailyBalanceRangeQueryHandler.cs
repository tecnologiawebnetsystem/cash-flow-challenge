using CashFlow.Application.DTOs;
using CashFlow.Domain.Repositories;
using MediatR;

namespace CashFlow.Application.Consolidation.Queries.GetDailyBalanceRange;


public sealed class GetDailyBalanceRangeQueryHandler
    : IRequestHandler<GetDailyBalanceRangeQuery, IReadOnlyList<DailyBalanceDto>>
{
    private readonly IDailyBalanceRepository _dailyBalanceRepository;

    public GetDailyBalanceRangeQueryHandler(IDailyBalanceRepository dailyBalanceRepository)
    {
        _dailyBalanceRepository = dailyBalanceRepository;
    }

    public async Task<IReadOnlyList<DailyBalanceDto>> Handle(
        GetDailyBalanceRangeQuery request,
        CancellationToken cancellationToken)
    {
        var balances = await _dailyBalanceRepository.GetByDateRangeAsync(
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return balances
            .OrderBy(b => b.ReferenceDate)
            .Select(DailyBalanceDto.FromDomain)
            .ToList();
    }
}
