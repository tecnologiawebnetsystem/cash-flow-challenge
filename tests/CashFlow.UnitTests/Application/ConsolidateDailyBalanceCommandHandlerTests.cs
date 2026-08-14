using CashFlow.Application.Common.Interfaces;
using CashFlow.Application.Consolidation.Commands.ConsolidateDailyBalance;
using CashFlow.Domain.Entities;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace CashFlow.UnitTests.Application;

public class ConsolidateDailyBalanceCommandHandlerTests
{
    private static readonly DateOnly SomeDate = new(2026, 8, 14);
    private static readonly DateTime Now = new(2026, 8, 14, 23, 59, 0, DateTimeKind.Utc);

    private readonly Mock<ILaunchRepository> _launchRepository = new();
    private readonly Mock<IDailyBalanceRepository> _dailyBalanceRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly ConsolidateDailyBalanceCommandHandler _handler;

    public ConsolidateDailyBalanceCommandHandlerTests()
    {
        _dateTimeProvider.SetupGet(p => p.UtcNow).Returns(Now);

        _handler = new ConsolidateDailyBalanceCommandHandler(
            _launchRepository.Object,
            _dailyBalanceRepository.Object,
            _unitOfWork.Object,
            _dateTimeProvider.Object);
    }

    private static Launch CreateLaunch(decimal amount, LaunchType type) =>
        Launch.Create("Lançamento de teste", amount, type, SomeDate, Now);

    [Fact]
    public async Task Handle_ShouldSumCreditsAndDebitsSeparately()
    {
        var launches = new List<Launch>
        {
            CreateLaunch(100m, LaunchType.Credit),
            CreateLaunch(50m, LaunchType.Credit),
            CreateLaunch(30m, LaunchType.Debit),
        };

        _launchRepository
            .Setup(r => r.GetByDateAsync(SomeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(launches);
        _dailyBalanceRepository
            .Setup(r => r.GetByDateAsync(SomeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyBalance?)null);

        var result = await _handler.Handle(new ConsolidateDailyBalanceCommand(SomeDate), CancellationToken.None);

        result.TotalCredits.Should().Be(150m);
        result.TotalDebits.Should().Be(30m);
        result.ClosingBalance.Should().Be(120m);
        result.Status.Should().Be(ConsolidationStatus.Consolidated);
    }

    [Fact]
    public async Task Handle_ShouldProduceZeroedBalance_WhenThereAreNoLaunchesForTheDate()
    {
        _launchRepository
            .Setup(r => r.GetByDateAsync(SomeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Launch>());
        _dailyBalanceRepository
            .Setup(r => r.GetByDateAsync(SomeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyBalance?)null);

        var result = await _handler.Handle(new ConsolidateDailyBalanceCommand(SomeDate), CancellationToken.None);

        result.TotalCredits.Should().Be(0m);
        result.TotalDebits.Should().Be(0m);
        result.ClosingBalance.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_ShouldReuseExistingDailyBalance_InsteadOfCreatingANewOne()
    {
        var existingBalance = DailyBalance.CreatePending(SomeDate);

        _launchRepository
            .Setup(r => r.GetByDateAsync(SomeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Launch> { CreateLaunch(10m, LaunchType.Credit) });
        _dailyBalanceRepository
            .Setup(r => r.GetByDateAsync(SomeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBalance);

        var result = await _handler.Handle(new ConsolidateDailyBalanceCommand(SomeDate), CancellationToken.None);

        result.ReferenceDate.Should().Be(existingBalance.ReferenceDate);
        _dailyBalanceRepository.Verify(
            r => r.UpsertAsync(existingBalance, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CalledTwiceWithSameData_ShouldYieldTheSameResult()
    {
        _launchRepository
            .Setup(r => r.GetByDateAsync(SomeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Launch> { CreateLaunch(75m, LaunchType.Credit) });
        _dailyBalanceRepository
            .Setup(r => r.GetByDateAsync(SomeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyBalance?)null);

        var firstResult = await _handler.Handle(new ConsolidateDailyBalanceCommand(SomeDate), CancellationToken.None);
        var secondResult = await _handler.Handle(new ConsolidateDailyBalanceCommand(SomeDate), CancellationToken.None);

        secondResult.ClosingBalance.Should().Be(firstResult.ClosingBalance);
    }

    [Fact]
    public async Task Handle_ShouldPersistChanges()
    {
        _launchRepository
            .Setup(r => r.GetByDateAsync(SomeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Launch>());
        _dailyBalanceRepository
            .Setup(r => r.GetByDateAsync(SomeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyBalance?)null);

        await _handler.Handle(new ConsolidateDailyBalanceCommand(SomeDate), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
