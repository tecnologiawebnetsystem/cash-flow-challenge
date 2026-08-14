using CashFlow.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CashFlow.UnitTests.Application;

public class InMemoryConsolidationQueueTests
{
    [Fact]
    public void TryEnqueue_ShouldReturnTrue_WhenQueueHasCapacity()
    {
        var queue = new InMemoryConsolidationQueue(NullLogger<InMemoryConsolidationQueue>.Instance);

        var accepted = queue.TryEnqueue(DateOnly.FromDateTime(DateTime.UtcNow));

        accepted.Should().BeTrue();
    }

    [Fact]
    public void TryEnqueue_ShouldNeverThrow_AndShouldReturnFalse_WhenQueueIsSaturated()
    {
        var queue = new InMemoryConsolidationQueue(NullLogger<InMemoryConsolidationQueue>.Instance);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var results = new List<bool>();
        for (var i = 0; i < 500; i++)
        {
            results.Add(queue.TryEnqueue(today));
        }

        results.Should().Contain(true);
        results.Should().Contain(false, "a fila deve descartar sinais excedentes em vez de crescer sem limite");
    }

    [Fact]
    public async Task ReadAllAsync_ShouldYield_EnqueuedItems_InOrder()
    {
        var queue = new InMemoryConsolidationQueue(NullLogger<InMemoryConsolidationQueue>.Instance);
        var day1 = new DateOnly(2024, 1, 1);
        var day2 = new DateOnly(2024, 1, 2);

        queue.TryEnqueue(day1);
        queue.TryEnqueue(day2);

        using var cts = new CancellationTokenSource();
        var received = new List<DateOnly>();
        try
        {
            await foreach (var date in queue.ReadAllAsync(cts.Token))
            {
                received.Add(date);
                if (received.Count == 2)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        received.Should().Equal(day1, day2);
    }
}
