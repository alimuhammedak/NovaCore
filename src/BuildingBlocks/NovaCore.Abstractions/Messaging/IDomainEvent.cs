using System.Diagnostics.CodeAnalysis;

namespace NovaCore.Abstractions.Messaging;

/// <summary>
/// Something that happened inside a module that other handlers may care about.
/// In-process today; the same contract can ride a message bus tomorrow without touching callers.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Identifies this occurrence of the event. Stable for the event's lifetime.</summary>
    Guid EventId { get; }

    /// <summary>When the event happened, in UTC.</summary>
    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>
/// Base for domain events: <see cref="EventId"/> and <see cref="OccurredOnUtc"/> are captured once, when the
/// event is created, so every reader (handlers, logs, the outbox) sees the same values.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc />
    /// <remarks>Defaults to a new version 7 (time-ordered) <see cref="Guid"/>.</remarks>
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    /// <inheritdoc />
    /// <remarks>Defaults to the moment the event is created.</remarks>
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Handles a specific <see cref="IDomainEvent"/>. Register it in DI, usually scoped. The outbox dispatcher runs it for
/// every saved event of its type, at least once: after a failure, or if the process stops before its success is
/// recorded, it runs again, so make it idempotent. The handlers of one event share a DI scope, as the handlers of one
/// request would: save what you change.
/// </summary>
/// <typeparam name="TEvent">The event type this handler reacts to.</typeparam>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "'Domain event handler' is the established domain term, and this is an interface, not an event-handler delegate (docs/plan/DECISIONS.md D-011, R-034).")]
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>Reacts to <paramref name="domainEvent"/>.</summary>
    /// <param name="domainEvent">The event that was raised.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
