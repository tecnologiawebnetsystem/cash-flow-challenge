using CashFlow.Application.Common.Interfaces;
using CashFlow.Application.DTOs;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CashFlow.Application.Launches.Commands.RegisterLaunch;

/// <summary>
/// Handles registration of a new launch. This is the write path that must
/// stay available and fast even when the consolidation subsystem is
/// degraded or unavailable: the launch is persisted first (source of
/// truth), and only afterwards is a best-effort signal sent to consolidate
/// its date. A dropped signal never loses financial data - see
/// <see cref="IConsolidationQueue"/>.
/// </summary>
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

        // Best-effort, non-blocking: the launch is already durably persisted
        // at this point, so failing to enqueue only delays visibility of the
        // consolidated balance - it never loses the launch itself.
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
