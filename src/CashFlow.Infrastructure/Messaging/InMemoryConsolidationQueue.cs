using System.Threading.Channels;
using CashFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CashFlow.Infrastructure.Messaging;

/// <summary>
/// In-process producer/consumer queue backed by <see cref="Channel{T}"/>.
///
/// Design rationale (documented here, referenced from the README):
/// registering a launch (the write path the business depends on for every
/// sale) must never be slowed down or blocked by the daily consolidation
/// subsystem. Using a bounded channel with <see cref="BoundedChannelFullMode.DropWrite"/>
/// gives us:
///   - Non-blocking producers: <see cref="TryEnqueue"/> never awaits and
///     never throws; it returns false instead of blocking when the queue
///     is saturated, so a slow/degraded consumer can never make
///     RegisterLaunchCommandHandler slow.
///   - A natural backpressure valve: at the documented peak of 50 req/s
///     with up to 5% acceptable loss, a bounded capacity intentionally
///     sheds the oldest-in-flight excess instead of growing memory
///     unbounded during a burst.
///   - A single dedicated background consumer (see ConsolidationWorker)
///     applies retry + circuit breaker policies without ever touching the
///     request path.
///
/// This is explicitly an in-memory, single-process mechanism. It is
/// documented in the README as the seam to swap for a real broker
/// (RabbitMQ/Azure Service Bus/SQS) if the application needs to scale
/// beyond a single instance or needs durable, cross-restart delivery.
/// </summary>
public sealed class InMemoryConsolidationQueue : IConsolidationQueue
{
    // Capacity sized well above the documented peak load (50 req/s) so that
    // only a genuinely sustained overload sheds signals - each dropped
    // signal only delays visibility of a balance, it never loses a launch.
    private const int Capacity = 200;

    private readonly Channel<DateOnly> _channel;
    private readonly ILogger<InMemoryConsolidationQueue> _logger;

    public InMemoryConsolidationQueue(ILogger<InMemoryConsolidationQueue> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<DateOnly>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public bool TryEnqueue(DateOnly date)
    {
        var accepted = _channel.Writer.TryWrite(date);

        if (!accepted)
        {
            _logger.LogWarning("Consolidation queue is at capacity ({Capacity}); dropping signal for {Date}.", Capacity, date);
        }

        return accepted;
    }

    public IAsyncEnumerable<DateOnly> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
