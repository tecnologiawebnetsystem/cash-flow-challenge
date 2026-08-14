namespace CashFlow.Domain.Enums;

/// <summary>
/// Status do ciclo de vida da consolidação de um saldo diário.
/// </summary>
public enum ConsolidationStatus
{
    /// <summary>Nenhuma consolidação foi executada ainda para esta data.</summary>
    Pending = 1,

    /// <summary>O saldo reflete a última execução de consolidação bem-sucedida.</summary>
    Consolidated = 2,

    /// <summary>A última tentativa de consolidação para esta data falhou.</summary>
    Failed = 3
}
