using CashFlow.Domain.Common;
using CashFlow.Domain.Enums;

namespace CashFlow.Domain.Entities;

/// <summary>
/// Saldo consolidado de uma data específica. É uma entidade derivada/projetada:
/// sempre recalculada por completo a partir dos <see cref="Launch"/> daquela data,
/// o que torna a consolidação idempotente e segura de repetir após uma falha
/// (requisito não funcional de resiliência do desafio).
///
/// Os totais aqui são <see cref="decimal"/> puros - e não <see cref="ValueObjects.Money"/> -
/// porque, diferente do valor de um lançamento individual, eles podem legitimamente
/// ser zero (dia sem lançamentos) ou o saldo de fechamento pode ser negativo
/// (débitos maiores que créditos).
/// </summary>
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

    /// <summary>
    /// Token de concorrência otimista (mapeado como rowversion pelo EF Core).
    /// Evita que duas consolidações concorrentes para a mesma data se
    /// sobrescrevam silenciosamente sob alta carga.
    /// </summary>
    public byte[]? RowVersion { get; private set; }

    // Necessário para materialização pelo EF Core.
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

    /// <summary>
    /// Aplica o resultado de um recálculo completo para esta data. A consolidação
    /// sempre substitui os totais anteriores por completo - nunca os incrementa -
    /// então executá-la novamente (ex.: após um retry) é seguro (idempotente).
    /// </summary>
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
