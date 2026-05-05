namespace CryptidCare.Claims.Domain.Common;

/// <summary>
/// Base class for domain entities with identity and auditing capabilities.
/// All aggregate roots should inherit from this to ensure consistent patterns.
/// </summary>
public abstract class Entity
{
    /// <summary>Primary key - unique identifier.</summary>
    public Guid Id { get; protected init; }

    /// <summary>UTC timestamp when the entity was created.</summary>
    public DateTime CreatedAtUtc { get; protected init; }

    /// <summary>UTC timestamp when the entity was last modified.</summary>
    public DateTime? ModifiedAtUtc { get; protected set; }

    /// <summary>User or system that created the entity (for audit trails).</summary>
    public string CreatedBy { get; protected init; } = "System";

    /// <summary>User or system that last modified the entity (for audit trails).</summary>
    public string? ModifiedBy { get; protected set; }

    /// <summary>Soft delete flag - allows logical deletion without data loss.</summary>
    public bool IsDeleted { get; protected set; }

    /// <summary>UTC timestamp of soft delete, if applicable.</summary>
    public DateTime? DeletedAtUtc { get; protected set; }

    /// <summary>User or system that deleted the entity.</summary>
    public string? DeletedBy { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
        CreatedAtUtc = DateTime.UtcNow;
    }

    protected Entity(Guid id)
    {
        Id = id;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Marks the entity as deleted (soft delete).</summary>
    public virtual void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        DeletedBy = deletedBy ?? CreatedBy;
    }

    /// <summary>Restores a soft-deleted entity.</summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }

    /// <summary>Records modification metadata.</summary>
    public virtual void RecordModification(string? modifiedBy = null)
    {
        ModifiedAtUtc = DateTime.UtcNow;
        ModifiedBy = modifiedBy ?? CreatedBy;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity entity)
        {
            return false;
        }

        return Id == entity.Id && GetType() == entity.GetType();
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}

/// <summary>
/// Value object base - immutable, equality-based objects with no identity.
/// </summary>
public abstract class ValueObject
{
    /// <summary>Gets the components that define equality for this value object.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is not ValueObject valueObject)
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(1, (current, obj) =>
            {
                return HashCode.Combine(current, obj);
            });
    }
}

/// <summary>
/// Aggregate root - boundary for transactional consistency within domain models.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <summary>Gets the domain events that occurred within this aggregate.</summary>
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }

    protected AggregateRoot(Guid id) : base(id) { }

    /// <summary>Registers a domain event to be published.</summary>
    protected void RaiseDomainEvent(DomainEvent @event) => _domainEvents.Add(@event);

    /// <summary>Clears the domain events (typically after persistence).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Base class for domain events - immutable records of something that happened.
/// </summary>
public abstract record DomainEvent
{
    /// <summary>When the event occurred.</summary>
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;

    /// <summary>Correlation ID for distributed tracing.</summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
