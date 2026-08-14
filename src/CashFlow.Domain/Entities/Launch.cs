using CashFlow.Domain.Common;
using CashFlow.Domain.Enums;
using CashFlow.Domain.Events;
using CashFlow.Domain.Exceptions;
using CashFlow.Domain.ValueObjects;

namespace CashFlow.Domain.Entities;

/// <summary>
/// Um único lançamento financeiro: um crédito ou um débito ocorrido em uma
/// determinada data. Esta é a única fonte da verdade do fluxo de caixa - os
/// saldos diários são sempre uma projeção derivada/consolidada dos
/// lançamentos, nunca o contrário.
/// </summary>
public sealed class Launch : AggregateRoot
{
    public string Description { get; private set; }
    public Money Amount { get; private set; }
    public LaunchType Type { get; private set; }
    public DateOnly LaunchDate { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    // Necessário para materialização pelo EF Core.
    private Launch()
    {
        Description = string.Empty;
        Amount = null!;
    }

    private Launch(Guid id, string description, Money amount, LaunchType type, DateOnly launchDate, DateTime createdAtUtc)
        : base(id)
    {
        Description = description;
        Amount = amount;
        Type = type;
        LaunchDate = launchDate;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Método de fábrica (encapsula os invariantes e garante que um Launch
    /// nunca exista em estado inválido) que cria um novo lançamento e
    /// dispara um <see cref="LaunchRegisteredEvent"/> para que assinantes
    /// interessados (ex.: o subsistema de consolidação) possam reagir.
    /// </summary>
    public static Launch Create(string description, decimal amount, LaunchType type, DateOnly launchDate, DateTime nowUtc)
    {
        var normalizedDescription = (description ?? string.Empty).Trim();
        if (normalizedDescription.Length == 0 || normalizedDescription.Length > 200)
        {
            throw new InvalidLaunchDescriptionException();
        }

        var money = Money.Create(amount);

        var launch = new Launch(Guid.NewGuid(), normalizedDescription, money, type, launchDate, nowUtc);
        launch.RaiseDomainEvent(new LaunchRegisteredEvent(launch.Id, launch.LaunchDate, launch.Amount.Amount, launch.Type));

        return launch;
    }

    /// <summary>
    /// A contribuição com sinal deste lançamento para um saldo diário:
    /// positiva para créditos, negativa para débitos.
    /// </summary>
    public decimal SignedAmount => Type == LaunchType.Credit ? Amount.Amount : -Amount.Amount;
}
