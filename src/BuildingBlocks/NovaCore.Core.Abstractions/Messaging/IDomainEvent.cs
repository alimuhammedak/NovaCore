namespace NovaCore.Core.Abstractions.Messaging;

/// <summary>
/// Something that happened inside a module that other handlers may care about.
/// In-process today; the same contract can ride a message bus tomorrow without touching callers.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>
/// Base for domain events: <see cref="EventId"/> and <see cref="OccurredOnUtc"/> are captured once, when the
/// event is created, so every reader (handlers, logs, a future outbox) sees the same values.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Handles a specific <see cref="IDomainEvent"/>. Resolved and invoked by the dispatcher.</summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>Publishes domain events to every registered handler. The single seam for in-process eventing.</summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
