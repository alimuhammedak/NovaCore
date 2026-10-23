using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NovaCore.Abstractions.Messaging;
using NovaCore.EntityFrameworkCore.Persistence;

namespace NovaCore.EntityFrameworkCore.Messaging;

/// <summary>
/// Delivers the events waiting in every module's outbox to their <see cref="IDomainEventHandler{TEvent}"/>s: a pass
/// when the host starts, then one every <see cref="PollingInterval"/>, oldest message first. A message that fails is
/// retried on a later pass, after newer ones, so there is no ordering guarantee.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Each message gets a DI scope for its handlers, which they share.</item>
/// <item>A handler that finishes is recorded in <c>outbox_consumers</c>, and a retry of the message skips it. Once
/// every handler has finished, the message is marked processed.</item>
/// <item>A failure counts an attempt; the next pass tries again. After <see cref="MaxAttempts"/> the message stays in
/// the outbox for inspection.</item>
/// <item>Delivery is at least once: if the process stops between a handler's own save and its record, the handler runs
/// again. Handlers must be idempotent.</item>
/// <item>No transaction is opened here, so each query and write is a retriable unit of the context's execution
/// strategy.</item>
/// <item>One dispatcher per database is assumed: two hosts over the same database could both deliver a message.</item>
/// <item>A processed message stays for <see cref="ProcessedRetention"/>, for inspection, and is then deleted with its
/// handlers' records. A message left after <see cref="MaxAttempts"/> stays until someone removes it.</item>
/// </list>
/// </remarks>
internal sealed partial class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IEnumerable<ModuleDbContextDescriptor> contexts,
    DomainEventHandlerInvokers invokers,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    /// <summary>How long the dispatcher waits between passes.</summary>
    public static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    /// <summary>The deliveries a message gets before it is left for inspection.</summary>
    public const int MaxAttempts = 10;

    /// <summary>The longest failure text kept in <see cref="OutboxMessage.Error"/>.</summary>
    public const int MaxErrorLength = 2000;

    /// <summary>The longest handler type name <see cref="OutboxConsumer.Handler"/> keeps.</summary>
    public const int MaxHandlerLength = 256;

    /// <summary>
    /// How long a processed message is kept. A message is only delivered again while it waits in the outbox, so the
    /// records aren't needed after that; the time is for looking at what happened.
    /// </summary>
    public static readonly TimeSpan ProcessedRetention = TimeSpan.FromDays(7);

    private const int BatchSize = 50;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // A PeriodicTimer keeps a tick that fires during a pass, so no interval is lost.
        using var timer = new PeriodicTimer(PollingInterval, timeProvider);
        do
        {
            foreach (var context in contexts)
                await DispatchAsync(context.ContextType, stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task DispatchAsync(Type contextType, CancellationToken cancellationToken)
    {
        try
        {
            var scope = scopeFactory.CreateAsyncScope();
            await using var scopeDisposal = scope.ConfigureAwait(false);
            var outbox = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
            if (outbox.Model.FindEntityType(typeof(OutboxMessage)) is null)
                return;

            // Keyset batches: a message that fails in this pass is not read again until the next one.
            var after = 0L;
            List<OutboxMessage> batch;
            do
            {
                batch = await outbox.Set<OutboxMessage>()
                    .AsNoTracking()
                    .Where(message => message.ProcessedOnUtc == null && message.Attempts < MaxAttempts && message.Id > after)
                    .OrderBy(message => message.Id)
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (var message in batch)
                {
                    await DeliverAsync(outbox, message, cancellationToken).ConfigureAwait(false);
                    after = message.Id;
                }
            }
            while (batch.Count == BatchSize);

            await DeleteExpiredAsync(outbox, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // The loop's boundary: an unreachable database or a missing table must not stop the host. Only the host's own
            // stop gets through.
            LogPassFailed(contextType.Name, exception);
        }
    }

    private async Task DeliverAsync(DbContext outbox, OutboxMessage message, CancellationToken cancellationToken)
    {
        var row = outbox.Set<OutboxMessage>().Where(stored => stored.Id == message.Id);
        try
        {
            var domainEvent = Deserialize(message);
            var invoker = invokers.For(domainEvent.GetType());
            var finished = await outbox.Set<OutboxConsumer>()
                .AsNoTracking()
                .Where(consumer => consumer.MessageId == message.Id)
                .Select(consumer => consumer.Handler)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // The handlers' scope is not the bookkeeping context's: a handler's unsaved changes can't be saved with a record.
            var scope = scopeFactory.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                foreach (var handler in invoker.GetHandlers(scope.ServiceProvider))
                {
                    var name = handler.GetType().FullName!;
                    if (finished.Contains(name))
                        continue;

                    // The record is what keeps a retry from running the handler again: a handler that can't have one
                    // doesn't run.
                    if (name.Length > MaxHandlerLength)
                        throw new InvalidOperationException(
                            $"The handler type name '{name}' is longer than the {MaxHandlerLength} characters its outbox record keeps.");

                    await invoker.InvokeAsync(handler, domainEvent, cancellationToken).ConfigureAwait(false);

                    outbox.Add(new OutboxConsumer
                    {
                        MessageId = message.Id,
                        Handler = name,
                        ProcessedOnUtc = timeProvider.GetUtcNow()
                    });
                    await outbox.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    // The pass's context stays empty, and a second registration of the same handler type is skipped.
                    outbox.ChangeTracker.Clear();
                    finished.Add(name);
                }
            }

            var processedOnUtc = timeProvider.GetUtcNow();
            await row.ExecuteUpdateAsync(set => set.SetProperty(stored => stored.ProcessedOnUtc, processedOnUtc), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // A record that failed to save must not ride along with the next one.
            outbox.ChangeTracker.Clear();

            var error = $"{exception.GetType().FullName}: {exception.Message}";
            if (error.Length > MaxErrorLength)
                error = error[..MaxErrorLength];
            await row.ExecuteUpdateAsync(set => set
                .SetProperty(stored => stored.Attempts, stored => stored.Attempts + 1)
                .SetProperty(stored => stored.Error, error), cancellationToken)
                .ConfigureAwait(false);

            var attempt = message.Attempts + 1;
            if (attempt < MaxAttempts)
                LogDeliveryFailed(message.EventId, message.Type, attempt, MaxAttempts, exception);
            else
                LogDeliveryAbandoned(message.EventId, message.Type, attempt, exception);
        }
    }

    // The processed messages older than the retention, a batch per statement until a short one: the (ProcessedOnUtc, Id)
    // index finds them, and a message left for inspection was never processed, so it stays. The records go with their
    // message (cascade).
    private async Task DeleteExpiredAsync(DbContext outbox, CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - ProcessedRetention;
        int deleted;
        do
        {
            deleted = await outbox.Set<OutboxMessage>()
                .Where(message => message.ProcessedOnUtc < cutoff)
                .OrderBy(message => message.ProcessedOnUtc)
                .Take(BatchSize)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        while (deleted == BatchSize);
    }

    private static IDomainEvent Deserialize(OutboxMessage message)
    {
        var eventType = Type.GetType(message.Type, throwOnError: false);
        if (eventType is null || !typeof(IDomainEvent).IsAssignableFrom(eventType))
            throw new InvalidOperationException($"'{message.Type}' is not a domain event type this host can load.");

        return (IDomainEvent?)JsonSerializer.Deserialize(message.Content, eventType)
            ?? throw new InvalidOperationException("The outbox message has no content.");
    }

    [LoggerMessage(EventId = 21, Level = LogLevel.Warning,
        Message = "Delivering outbox message {EventId} ({EventType}) failed on attempt {Attempt} of {MaxAttempts}. The next pass retries it.")]
    private partial void LogDeliveryFailed(Guid eventId, string eventType, int attempt, int maxAttempts, Exception exception);

    [LoggerMessage(EventId = 22, Level = LogLevel.Error,
        Message = "Delivering outbox message {EventId} ({EventType}) failed {Attempts} times. It stays in the outbox for inspection and is not retried.")]
    private partial void LogDeliveryAbandoned(Guid eventId, string eventType, int attempts, Exception exception);

    [LoggerMessage(EventId = 23, Level = LogLevel.Error, Message = "Dispatching the {Context} outbox failed. The next pass tries again.")]
    private partial void LogPassFailed(string context, Exception exception);
}
