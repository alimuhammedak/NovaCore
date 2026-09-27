namespace NovaCore.EntityFrameworkCore.Messaging;

/// <summary>A domain event in a module's outbox (<c>outbox_messages</c>), written by <see cref="OutboxInterceptor"/>.</summary>
internal sealed class OutboxMessage
{
    /// <summary>
    /// Database-generated and ever-increasing: the clustered key and the dispatch order. The event id can't be either:
    /// SQL Server orders <c>uniqueidentifier</c> by its last bytes, not by a version 7 GUID's timestamp.
    /// </summary>
    public long Id { get; init; }

    /// <summary>The event's <see cref="Abstractions.Messaging.IDomainEvent.EventId"/>; unique in the outbox.</summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// The event's CLR type: <c>"Namespace.Type, Assembly"</c>, without the assembly's version. A generic event's type
    /// arguments keep their full assembly-qualified names, versions included.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>The event serialized with System.Text.Json as its runtime type.</summary>
    public required string Content { get; init; }

    /// <summary>The event's <see cref="Abstractions.Messaging.IDomainEvent.OccurredOnUtc"/>.</summary>
    public required DateTimeOffset OccurredOnUtc { get; init; }

    /// <summary>When every handler had handled the event; null while it waits in the outbox.</summary>
    public DateTimeOffset? ProcessedOnUtc { get; init; }

    /// <summary>The failed deliveries so far. At <see cref="OutboxDispatcher.MaxAttempts"/> the message is no longer tried.</summary>
    public int Attempts { get; init; }

    /// <summary>The last failure: the exception's type and message.</summary>
    public string? Error { get; init; }
}
