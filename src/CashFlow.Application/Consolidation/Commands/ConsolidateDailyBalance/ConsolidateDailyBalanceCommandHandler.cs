using CashFlow.Application.Common.Interfaces;
using CashFlow.Application.DTOs;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Repositories;
using MediatR;

namespace CashFlow.Application.Consolidation.Commands.ConsolidateDailyBalance;

/// <summary>
/// This handler intentionally does NOT catch/swallow exceptions: retry and
/// circuit-breaker policies are applied by the Infrastructure caller (the
/// background worker), keeping resilience concerns out of the business
/// logic (SRP) while still guaranteeing the launch-registration flow is
/// never blocked by a failure here.
/// </summary>
public sealed class ConsolidateDailyBalanceCommandHandler
    : IRequestHandler<ConsolidateDailyBalanceCommand, DailyBalanceDto>
{
    private readonly ILaunchRepository _launchRepository;
    private readonly IDailyBalanceRepository _dailyBalanceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConsolidateDailyBalanceCommandHandler(
        ILaunchRepository launchRepository,
        IDailyBalanceRepository dailyBalanceRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _launchRepository = launchRepository;
        _dailyBalanceRepository = dailyBalanceRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DailyBalanceDto> Handle(ConsolidateDailyBalanceCommand request, CancellationToken cancellationToken)
    {
        var launches = await _launchRepository.GetByDateAsync(request.Date, cancellationToken);

        var totalCredits = launches.Where(l => l.SignedAmount > 0).Sum(l => l.SignedAmount);
        var totalDebits = launches.Where(l => l.SignedAmount < 0).Sum(l => -l.SignedAmount);

        var dailyBalance = await _dailyBalanceRepository.GetByDateAsync(request.Date, cancellationToken)
                            ?? DailyBalance.CreatePending(request.Date);

        dailyBalance.Consolidate(totalCredits, totalDebits, _dateTimeProvider.UtcNow);

        await _dailyBalanceRepository.UpsertAsync(dailyBalance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DailyBalanceDto.FromDomain(dailyBalance);
    }
}
