using CashFlow.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CashFlow.UnitTests.Application;

/// <summary>
/// These tests exercise the queue directly (rather than mocking it) because
/// its capacity/back-pressure behavior is itself the business rule under
/// test: the write path must never block, and overflow must be shed instead
/// of buffered without bound.
/// </summary>
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

        // Saturate the bounded channel (capacity is documented as 200)
        // without ever draining it, simulating a consumer outage.
        var results = new List<bool>();
        for (var i = 0; i < 500; i++)
        {
            results.Add(queue.TryEnqueue(today));
        }

        results.Should().Contain(true);
        results.Should().Contain(false, "the queue must shed excess signals instead of growing without bound");
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

        // Cancelar o token durante a leitura é o mecanismo real usado para
        // encerrar o worker no shutdown (ver ConsolidationWorker), e por
        // contrato do IAsyncEnumerable isso propaga uma OperationCanceledException
        // em vez de simplesmente finalizar o laço - por isso ela é esperada
        // e tratada aqui, e não um efeito colateral indesejado.
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
            // Esperado: é assim que sinalizamos o fim da leitura neste teste.
        }

        received.Should().Equal(day1, day2);
    }
}
