using CashFlow.Domain.Common;
using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Entities;


public sealed class DailyBalance : Entity
{
    public DateOnly ReferenceDate { get; private set; }
    public decimal TotalCredits { get; private set; }
    public decimal TotalDebits { get; private set; }
    public decimal ClosingBalance { get; private set; }
    public ConsolidationStatus Status { get; private set; }
    public DateTime? ConsolidatedAtUtc { get; private set; }
    public int FailedAttempts { get; private set; }
    public string? FailureReason { get; private set; }

    public byte[]? RowVersion { get; private set; }
    private DailyBalance()
    {
    }

    private DailyBalance(Guid id, DateOnly referenceDate)
        : base(id)
    {
        ReferenceDate = referenceDate;
        TotalCredits = 0m;
        TotalDebits = 0m;
        ClosingBalance = 0m;
        Status = ConsolidationStatus.Pending;
    }

    public static DailyBalance CreatePending(DateOnly referenceDate) =>
        new(Guid.NewGuid(), referenceDate);
    public void Consolidate(decimal totalCredits, decimal totalDebits, DateTime consolidatedAtUtc)
    {
        TotalCredits = totalCredits;
        TotalDebits = totalDebits;
        ClosingBalance = totalCredits - totalDebits;
        Status = ConsolidationStatus.Consolidated;
        ConsolidatedAtUtc = consolidatedAtUtc;
        FailedAttempts = 0;
        FailureReason = null;
    }

    public void MarkAsFailed(string reason)
    {
        Status = ConsolidationStatus.Failed;
        FailedAttempts++;
        FailureReason = reason;
    }
}
