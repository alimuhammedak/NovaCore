namespace NovaCore.EntityFrameworkCore.Messaging;

/// <summary>
/// A handler that has handled an outbox message (<c>outbox_consumers</c>, keyed by the event and the handler). When the
/// dispatcher delivers the message again after a failure, it skips these handlers.
/// </summary>
internal sealed class OutboxConsumer
{
    /// <summary>The handled event's <see cref="OutboxMessage.EventId"/>.</summary>
    public required Guid EventId { get; init; }

    /// <summary>The handler's full type name.</summary>
    public required string Handler { get; init; }

    /// <summary>When the handler finished.</summary>
    public required DateTimeOffset ProcessedOnUtc { get; init; }
}
