namespace CashFlow.Domain.Common;

/// <summary>
/// Classe base para entidades com semântica de comparação por identidade.
/// Entidades são comparadas pela identidade (Id), não pelos valores de seus
/// atributos.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected init; }

    /// <summary>
    /// Construtor sem parâmetros exigido pelo materializador do EF Core.
    /// Não é destinado ao uso direto pelo código da aplicação - sempre crie
    /// entidades por meio de seus métodos de fábrica nomeados (ex.:
    /// <c>Launch.Create</c>).
    /// </summary>
    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("O identificador da entidade não pode ser vazio.", nameof(id));
        }

        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
