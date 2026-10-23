namespace EnglishAdventure.Domain.Common;

/// <summary>
/// Base class for business entities: a <see cref="Guid"/> identity and equality by that identity. Two entities of the
/// same type with the same <see cref="Id"/> are the same business object, whatever their other values.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected set; }

    public bool Equals(Entity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // An entity without an identity yet (Guid.Empty) is only equal to itself.
        return other.GetType() == GetType()
            && Id != Guid.Empty
            && Id == other.Id;
    }

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => Id == Guid.Empty ? base.GetHashCode() : HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
