namespace CashFlow.Domain.Enums;

/// <summary>
/// Lifecycle status of a daily balance consolidation.
/// </summary>
public enum ConsolidationStatus
{
    /// <summary>No consolidation has ever run for this date yet.</summary>
    Pending = 1,

    /// <summary>The balance reflects the last successful consolidation run.</summary>
    Consolidated = 2,

    /// <summary>The last consolidation attempt for this date failed.</summary>
    Failed = 3
}
