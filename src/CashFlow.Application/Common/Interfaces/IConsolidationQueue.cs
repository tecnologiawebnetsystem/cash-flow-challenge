namespace CashFlow.Application.Common.Interfaces;

public interface IConsolidationQueue
{

    bool TryEnqueue(DateOnly date);

    IAsyncEnumerable<DateOnly> ReadAllAsync(CancellationToken cancellationToken);
}
