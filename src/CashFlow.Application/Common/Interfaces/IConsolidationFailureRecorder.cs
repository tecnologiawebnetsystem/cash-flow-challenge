namespace CashFlow.Application.Common.Interfaces;


public interface IConsolidationFailureRecorder
{
    Task RecordFailureAsync(DateOnly date, string reason, CancellationToken cancellationToken);
}
