using System.Threading.Channels;
using CashFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CashFlow.Infrastructure.Messaging;


public sealed class InMemoryConsolidationQueue : IConsolidationQueue
{
    private const int Capacity = 200;

    private readonly Channel<DateOnly> _channel;
    private readonly ILogger<InMemoryConsolidationQueue> _logger;

    public InMemoryConsolidationQueue(ILogger<InMemoryConsolidationQueue> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<DateOnly>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public bool TryEnqueue(DateOnly date)
    {
        var accepted = _channel.Writer.TryWrite(date);

        if (!accepted)
        {
            _logger.LogWarning("Fila de consolidação em capacidade máxima ({Capacity}); sinal descartado para {Date}.", Capacity, date);
        }

        return accepted;
    }

    public IAsyncEnumerable<DateOnly> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
