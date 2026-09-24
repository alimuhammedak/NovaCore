using NovaCore.Core.Abstractions.Messaging;

namespace NovaCore.Core.Abstractions.Entities;

/// <summary>
/// Base class for entities. Carries identity and a buffer of domain events that are dispatched
/// after the unit of work commits.
/// </summary>
public abstract class Entity<TId> : IEntity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(TId id) => Id = id;

    // Required by EF Core's materializer.
    protected Entity() => Id = default!;

    public TId Id { get; protected set; }

    /// <summary>Domain events raised but not yet dispatched. Read-only to the outside world.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>The common case: an auditable aggregate root keyed by <see cref="Guid"/>.</summary>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity
    where TId : notnull
{
    protected AuditableEntity(TId id) : base(id) { }
    protected AuditableEntity() { }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}
