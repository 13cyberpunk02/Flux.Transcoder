namespace Flux.Transcoder.Domain.Common;

public abstract class Entity(Guid id)
{
    public Guid Id { get; protected set; } = id;

    protected Entity() : this(Guid.CreateVersion7())
    {
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}