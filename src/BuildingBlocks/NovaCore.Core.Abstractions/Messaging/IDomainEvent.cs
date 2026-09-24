namespace NovaCore.Core.Abstractions.Messaging;

/// <summary>
/// Something that happened inside a module that other handlers may care about.
/// In-process today; the same contract can ride a message bus tomorrow without touching callers.
/// </summary>
public interface IDomainEvent
{
    Guid EventId => Guid.CreateVersion7();
    DateTimeOffset OccurredOnUtc => DateTimeOffset.UtcNow;
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
