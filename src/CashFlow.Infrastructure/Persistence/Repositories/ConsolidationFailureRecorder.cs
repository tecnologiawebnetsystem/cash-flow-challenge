using CashFlow.Application.Common.Interfaces;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Repositories;

namespace CashFlow.Infrastructure.Persistence.Repositories;

public sealed class ConsolidationFailureRecorder : IConsolidationFailureRecorder
{
    private readonly IDailyBalanceRepository _dailyBalanceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConsolidationFailureRecorder(IDailyBalanceRepository dailyBalanceRepository, IUnitOfWork unitOfWork)
    {
        _dailyBalanceRepository = dailyBalanceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task RecordFailureAsync(DateOnly date, string reason, CancellationToken cancellationToken)
    {
        var dailyBalance = await _dailyBalanceRepository.GetByDateAsync(date, cancellationToken)
                            ?? DailyBalance.CreatePending(date);

        dailyBalance.MarkAsFailed(reason);

        await _dailyBalanceRepository.UpsertAsync(dailyBalance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
