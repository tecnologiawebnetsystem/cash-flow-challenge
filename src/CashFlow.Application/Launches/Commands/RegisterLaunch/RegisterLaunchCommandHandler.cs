using CashFlow.Application.Common.Interfaces;
using CashFlow.Application.DTOs;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CashFlow.Application.Launches.Commands.RegisterLaunch;

public sealed class RegisterLaunchCommandHandler : IRequestHandler<RegisterLaunchCommand, LaunchDto>
{
    private readonly ILaunchRepository _launchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConsolidationQueue _consolidationQueue;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RegisterLaunchCommandHandler> _logger;

    public RegisterLaunchCommandHandler(
        ILaunchRepository launchRepository,
        IUnitOfWork unitOfWork,
        IConsolidationQueue consolidationQueue,
        IDateTimeProvider dateTimeProvider,
        ILogger<RegisterLaunchCommandHandler> logger)
    {
        _launchRepository = launchRepository;
        _unitOfWork = unitOfWork;
        _consolidationQueue = consolidationQueue;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<LaunchDto> Handle(RegisterLaunchCommand request, CancellationToken cancellationToken)
    {
        var launchDate = request.LaunchDate ?? _dateTimeProvider.Today;

        var launch = Launch.Create(
            request.Description,
            request.Amount,
            request.Type,
            launchDate,
            _dateTimeProvider.UtcNow);

        await _launchRepository.AddAsync(launch, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Best-effort, não bloqueante: o lançamento já está persistido de
        // forma durável neste ponto, então falhar ao enfileirar apenas
        // atrasa a visibilidade do saldo consolidado - nunca perde o
        // lançamento em si.
        if (!_consolidationQueue.TryEnqueue(launch.LaunchDate))
        {
            _logger.LogWarning(
                "Consolidation queue is full; dropped consolidation signal for {LaunchDate}. " +
                "The periodic reconciliation job will pick it up.",
                launch.LaunchDate);
        }

        launch.ClearDomainEvents();

        return LaunchDto.FromDomain(launch);
    }
}
