using NovaCore.Abstractions.Messaging;

namespace NovaCore.Abstractions.Entities;

/// <summary>
/// The root of every entity: the buffer of domain events it raised. It has no key type, so the persistence layer can read
/// the events of any entity: when the entity is saved, they are written to its module's outbox in the same transaction.
/// Derive from <see cref="Entity{TId}"/>.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>For <see cref="Entity{TId}"/>, which adds the identity.</summary>
    protected Entity() { }

    /// <summary>Domain events raised and not yet saved. Read-only to the outside world.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Records a domain event. Saving the entity writes it to the module's outbox.</summary>
    /// <param name="domainEvent">The event that happened.</param>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Forgets the buffered events, typically once they have been written to the outbox.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>Base class for entities: an identity, plus the domain-event buffer of <see cref="Entity"/>.</summary>
/// <typeparam name="TId">The type of the identity.</typeparam>
public abstract class Entity<TId> : Entity, IEntity<TId>
    where TId : notnull
{
    /// <summary>Creates an entity with the given identity.</summary>
    /// <param name="id">The identity.</param>
    protected Entity(TId id) => Id = id;

    /// <summary>For EF Core's materializer, which sets <see cref="Id"/> afterwards.</summary>
    protected Entity() => Id = default!;

    /// <inheritdoc />
    public TId Id { get; protected set; }
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
