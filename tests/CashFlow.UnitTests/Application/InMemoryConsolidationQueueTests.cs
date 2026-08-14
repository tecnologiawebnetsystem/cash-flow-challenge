using CashFlow.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CashFlow.UnitTests.Application;

/// <summary>
/// Estes testes exercitam a fila diretamente (em vez de mocá-la) porque o
/// seu comportamento de capacidade/back-pressure é a própria regra de
/// negócio sob teste: o caminho de escrita nunca deve bloquear, e o
/// excedente deve ser descartado em vez de armazenado sem limite.
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

        // Satura o canal limitado (capacidade documentada como 200) sem
        // nunca esvaziá-lo, simulando uma indisponibilidade do consumidor.
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
