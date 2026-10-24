using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NovaCore.Abstractions.Entities;
using NovaCore.EntityFrameworkCore.MultiTenancy;
using NovaCore.EntityFrameworkCore.Persistence;

namespace NovaCore.EntityFrameworkCore.Messaging;

/// <summary>
/// Writes the domain events raised on a context's entities to its outbox (<see cref="OutboxModelBuilderExtensions.AddOutbox"/>)
/// as part of the SaveChanges that saves the entities, then clears the entities' buffers. EF Core runs this before it
/// detects changes and saves, so the outbox rows share the save's transaction: the events are committed with the entity
/// changes or not at all. <see cref="PersistenceExtensions.AddModuleDbContext{TContext}"/> adds it to every module context.
/// If a context's entities raise events but its model has no outbox, the save fails rather than drop them. A row keeps the
/// saving scope's tenant, which the handlers then run in, and the W3C trace context of the activity the save runs in (a
/// request's, say), which their span continues.
/// The buffers are cleared as the events join the save. If the save fails, their rows stay in that context like the
/// entity changes do: retry <c>SaveChanges</c> on the same context. An entity re-added to another context has no events
/// left.
/// </summary>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    /// <summary>The length of a W3C <c>traceparent</c> (version 00), the longest one a row keeps.</summary>
    internal const int MaxTraceParentLength = 55;

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        WriteToOutbox(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        WriteToOutbox(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void WriteToOutbox(DbContext? context)
    {
        if (context is null)
            return;

        // Entries runs DetectChanges, which starts tracking entities added through navigations: their events count too.
        var raisers = context.ChangeTracker.Entries<Entity>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();
        if (raisers.Count == 0)
            return;

        if (context.Model.FindEntityType(typeof(OutboxMessage)) is null)
            throw new InvalidOperationException(
                $"Entities of {context.GetType().Name} raised domain events, but its model has no outbox. " +
                "Call modelBuilder.AddOutbox() in OnModelCreating and add a migration.");

        // The same for every event of the save: the scope's tenant and the trace the save is part of.
        var tenant = TenantQueryFilter.TenantOf(context);
        var traceParent = Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity
                          && activity.Id is { Length: <= MaxTraceParentLength } id
            ? id
            : null;

        foreach (var entity in raisers)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                var eventType = domainEvent.GetType();
                context.Add(new OutboxMessage
                {
                    EventId = domainEvent.EventId,
                    Type = $"{eventType.FullName}, {eventType.Assembly.GetName().Name}",
                    Content = JsonSerializer.Serialize(domainEvent, eventType),
                    OccurredOnUtc = domainEvent.OccurredOnUtc,
                    TenantId = tenant,
                    TraceParent = traceParent
                });
            }

            entity.ClearDomainEvents();
        }
    }
}
