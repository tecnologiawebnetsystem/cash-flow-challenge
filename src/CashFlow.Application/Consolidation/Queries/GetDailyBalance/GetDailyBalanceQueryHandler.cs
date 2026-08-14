using CashFlow.Application.Common.Exceptions;
using CashFlow.Application.DTOs;
using CashFlow.Domain.Repositories;
using MediatR;

namespace CashFlow.Application.Consolidation.Queries.GetDailyBalance;

public sealed class GetDailyBalanceQueryHandler : IRequestHandler<GetDailyBalanceQuery, DailyBalanceDto>
{
    private readonly IDailyBalanceRepository _dailyBalanceRepository;

    public GetDailyBalanceQueryHandler(IDailyBalanceRepository dailyBalanceRepository)
    {
        _dailyBalanceRepository = dailyBalanceRepository;
    }

    public async Task<DailyBalanceDto> Handle(GetDailyBalanceQuery request, CancellationToken cancellationToken)
    {
        var dailyBalance = await _dailyBalanceRepository.GetByDateAsync(request.Date, cancellationToken)
                            ?? throw NotFoundException.ForDailyBalance(request.Date);

        return DailyBalanceDto.FromDomain(dailyBalance);
    }
}
