using CashFlow.Domain.Common;
using CashFlow.Domain.Exceptions;

namespace CashFlow.Domain.ValueObjects;

/// <summary>
/// Representa o valor monetário de um único lançamento.
/// Imutável e autovalidável: é impossível construir uma instância de
/// <see cref="Money"/> com um valor zero ou negativo, o que mantém esse
/// invariante em um único lugar em vez de espalhado pelo código.
///
/// Este tipo propositalmente não possui operadores aritméticos
/// (Add/Subtract): totais agregados como os créditos, débitos e saldo de
/// fechamento de um saldo diário são legitimamente zero ou negativos,
/// então são modelados como <see cref="decimal"/> puro em
/// <see cref="Entities.DailyBalance"/> em vez de forçar este invariante
/// mais estrito sobre eles.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; private init; }

    // Necessário para materialização pelo EF Core no mapeamento de owned type.
    private Money()
    {
    }

    private Money(decimal amount)
    {
        Amount = amount;
    }

    public static Money Create(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidLaunchAmountException(amount);
        }

        // Protege contra surpresas de arredondamento de ponto flutuante, normalizando para 2 casas decimais.
        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }

    public override string ToString() => Amount.ToString("F2");
}
