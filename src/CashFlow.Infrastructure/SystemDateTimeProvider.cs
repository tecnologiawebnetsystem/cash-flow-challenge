using CashFlow.Application.Common.Interfaces;

namespace CashFlow.Infrastructure;

/// <summary>
/// Encapsula os métodos estáticos de <see cref="DateTime"/>/<see cref="DateOnly"/>
/// atrás de uma interface, para que o código da Application (e seus testes
/// de unidade) nunca dependam diretamente do relógio do sistema.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
