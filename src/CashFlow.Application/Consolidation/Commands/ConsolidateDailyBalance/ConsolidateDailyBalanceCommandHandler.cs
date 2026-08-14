using CashFlow.Application.Common.Interfaces;
using CashFlow.Application.DTOs;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Repositories;
using MediatR;

namespace CashFlow.Application.Consolidation.Commands.ConsolidateDailyBalance;

/// <summary>
/// Este handler propositalmente NÃO captura/engole exceções: as políticas
/// de retry e circuit breaker são aplicadas pelo chamador na Infrastructure
/// (o worker em background), mantendo as preocupações de resiliência fora
/// da lógica de negócio (SRP), garantindo ainda que o fluxo de registro de
/// lançamentos nunca seja bloqueado por uma falha aqui.
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
