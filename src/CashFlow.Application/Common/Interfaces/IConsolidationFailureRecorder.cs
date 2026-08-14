namespace CashFlow.Application.Common.Interfaces;

/// <summary>
/// Persiste que a consolidação falhou para uma determinada data, depois que
/// toda tentativa de retry (e o circuit breaker, se aplicável) foi
/// esgotada. Mantida como uma interface enxuta e específica (Interface
/// Segregation) para que o caminho de falha do worker em background não
/// precise de toda a superfície de comandos/consultas.
/// </summary>
public interface IConsolidationFailureRecorder
{
    Task RecordFailureAsync(DateOnly date, string reason, CancellationToken cancellationToken);
}
