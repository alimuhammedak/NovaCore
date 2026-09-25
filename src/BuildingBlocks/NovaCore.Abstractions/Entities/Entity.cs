using NovaCore.Abstractions.Messaging;

namespace NovaCore.Abstractions.Entities;

/// <summary>
/// Base class for entities. Carries identity and a buffer of domain events that are dispatched
/// after the unit of work commits.
/// </summary>
/// <typeparam name="TId">The type of the identity.</typeparam>
public abstract class Entity<TId> : IEntity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Creates an entity with the given identity.</summary>
    /// <param name="id">The identity.</param>
    protected Entity(TId id) => Id = id;

    /// <summary>For EF Core's materializer, which sets <see cref="Id"/> afterwards.</summary>
    protected Entity() => Id = default!;

    /// <inheritdoc />
    public TId Id { get; protected set; }

    /// <summary>Domain events raised but not yet dispatched. Read-only to the outside world.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Records a domain event to be dispatched after the unit of work commits.</summary>
    /// <param name="domainEvent">The event that happened.</param>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Forgets the buffered events, typically once they have been dispatched.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>The common case: an auditable aggregate root keyed by <see cref="Guid"/>.</summary>
/// <typeparam name="TId">The type of the identity.</typeparam>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity
    where TId : notnull
{
    /// <summary>Creates an entity with the given identity.</summary>
    /// <param name="id">The identity.</param>
    protected AuditableEntity(TId id) : base(id) { }

    /// <summary>For EF Core's materializer.</summary>
    protected AuditableEntity() { }

    /// <inheritdoc />
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; set; }
}
