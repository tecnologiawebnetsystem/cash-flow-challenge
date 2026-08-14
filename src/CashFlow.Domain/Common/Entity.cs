namespace CashFlow.Domain.Common;

/// <summary>
/// Base class for entities with identity comparison semantics.
/// Entities are compared by identity (Id), not by their attribute values.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected init; }

    /// <summary>
    /// Parameterless constructor required by the EF Core materializer.
    /// Not intended for direct use by application code - always create
    /// entities through their named factory methods (e.g. <c>Launch.Create</c>).
    /// </summary>
    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entity identifier cannot be empty.", nameof(id));
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
