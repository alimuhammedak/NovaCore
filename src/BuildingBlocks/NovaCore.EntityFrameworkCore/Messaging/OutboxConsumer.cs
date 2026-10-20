namespace NovaCore.EntityFrameworkCore.Messaging;

/// <summary>
/// A handler that has handled an outbox message (<c>outbox_consumers</c>, keyed by the message and the handler). When the
/// dispatcher delivers the message again after a failure, it skips these handlers. Deleting the message deletes them.
/// </summary>
internal sealed class OutboxConsumer
{
    /// <summary>
    /// The handled message's <see cref="OutboxMessage.Id"/>: narrow and ever-increasing like the message's own key, where
    /// the event id is neither in SQL Server's order (docs/plan/RESEARCH.md R-068).
    /// </summary>
    public required long MessageId { get; init; }

    /// <summary>The handler's full type name.</summary>
    public required string Handler { get; init; }

    /// <summary>When the handler finished.</summary>
    public required DateTimeOffset ProcessedOnUtc { get; init; }
}
