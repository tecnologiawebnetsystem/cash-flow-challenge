using CashFlow.Application.Common.Interfaces;
using CashFlow.Application.Launches.Commands.RegisterLaunch;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CashFlow.UnitTests.Application;

public class RegisterLaunchCommandHandlerTests
{
    private static readonly DateOnly Today = new(2026, 8, 14);
    private static readonly DateTime Now = new(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILaunchRepository> _launchRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IConsolidationQueue> _consolidationQueue = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly RegisterLaunchCommandHandler _handler;

    public RegisterLaunchCommandHandlerTests()
    {
        _dateTimeProvider.SetupGet(p => p.Today).Returns(Today);
        _dateTimeProvider.SetupGet(p => p.UtcNow).Returns(Now);
        _consolidationQueue.Setup(q => q.TryEnqueue(It.IsAny<DateOnly>())).Returns(true);

        _handler = new RegisterLaunchCommandHandler(
            _launchRepository.Object,
            _unitOfWork.Object,
            _consolidationQueue.Object,
            _dateTimeProvider.Object,
            Mock.Of<ILogger<RegisterLaunchCommandHandler>>());
    }

    [Fact]
    public async Task Handle_ShouldPersistTheLaunch_AndReturnItsDto()
    {
        var command = new RegisterLaunchCommand("Venda", 200m, LaunchType.Credit, LaunchDate: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Description.Should().Be("Venda");
        result.Amount.Should().Be(200m);
        result.Type.Should().Be(LaunchType.Credit);
        result.LaunchDate.Should().Be(Today);

        _launchRepository.Verify(r => r.AddAsync(It.IsAny<Launch>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseProvidedLaunchDate_WhenGiven()
    {
        var explicitDate = new DateOnly(2026, 8, 1);
        var command = new RegisterLaunchCommand("Venda antiga", 50m, LaunchType.Credit, explicitDate);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.LaunchDate.Should().Be(explicitDate);
    }

    [Fact]
    public async Task Handle_ShouldEnqueueConsolidationSignal_ForTheLaunchDate()
    {
        var command = new RegisterLaunchCommand("Venda", 200m, LaunchType.Credit, LaunchDate: null);

        await _handler.Handle(command, CancellationToken.None);

        _consolidationQueue.Verify(q => q.TryEnqueue(Today), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenTheConsolidationQueueIsFull()
    {
        _consolidationQueue.Setup(q => q.TryEnqueue(It.IsAny<DateOnly>())).Returns(false);
        var command = new RegisterLaunchCommand("Venda", 200m, LaunchType.Credit, LaunchDate: null);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _launchRepository.Verify(r => r.AddAsync(It.IsAny<Launch>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPersistTheLaunch_BeforeAttemptingToEnqueue()
    {
        var callOrder = new List<string>();
        _launchRepository
            .Setup(r => r.AddAsync(It.IsAny<Launch>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("persist"))
            .Returns(Task.CompletedTask);
        _consolidationQueue
            .Setup(q => q.TryEnqueue(It.IsAny<DateOnly>()))
            .Callback(() => callOrder.Add("enqueue"))
            .Returns(true);

        var command = new RegisterLaunchCommand("Venda", 200m, LaunchType.Credit, LaunchDate: null);
        await _handler.Handle(command, CancellationToken.None);

        callOrder.Should().Equal("persist", "enqueue");
    }
}
