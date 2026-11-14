using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NovaCore.Abstractions.Entities;
using NovaCore.EntityFrameworkCore.MultiTenancy;
using NovaCore.EntityFrameworkCore.Persistence;

namespace NovaCore.EntityFrameworkCore.Messaging;

/// <summary>
/// Writes the domain events raised on a context's entities to its outbox (<see cref="OutboxModelBuilderExtensions.AddOutbox"/>)
/// as part of the SaveChanges that saves the entities, and clears the entities' buffers once that save succeeds. EF Core
/// runs this before it
/// detects changes and saves, so the outbox rows share the save's transaction: the events are committed with the entity
/// changes or not at all. <see cref="PersistenceExtensions.AddModuleDbContext{TContext}"/> adds it to every module context.
/// If a context's entities raise events but its model has no outbox, the save fails rather than drop them. A row keeps the
/// saving scope's tenant, which the handlers then run in, and the W3C trace context of the activity the save runs in (a
/// request's, say), which their span continues.
/// If the save fails, the entities keep their events and their rows stay in that context like the entity changes do: a
/// retry on the same context writes each event once (a row already tracked for an event's id isn't added again), and an
/// entity added to another context brings its events with it. A save that succeeds inside a transaction that is later
/// rolled back has cleared them all the same.
/// </summary>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    /// <summary>The length of a W3C <c>traceparent</c> (version 00), the longest one a row keeps.</summary>
    internal const int MaxTraceParentLength = 55;

    // The interceptor is one instance for every context (a singleton), so each saving context's raisers are kept here
    // from SavingChanges to SavedChanges; an entry goes with its context.
    private readonly ConditionalWeakTable<DbContext, List<Entity>> _saving = [];

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

    /// <summary>Clears the buffers of the entities whose events the save wrote to the outbox.</summary>
    /// <param name="eventData">The context that saved.</param>
    /// <param name="result">The number of rows the save wrote.</param>
    /// <returns><paramref name="result"/>.</returns>
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ClearSavedEvents(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    /// <summary>Clears the buffers of the entities whose events the save wrote to the outbox.</summary>
    /// <param name="eventData">The context that saved.</param>
    /// <param name="result">The number of rows the save wrote.</param>
    /// <param name="cancellationToken">Not used: clearing the buffers does no I/O.</param>
    /// <returns><paramref name="result"/>.</returns>
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        ClearSavedEvents(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void ClearSavedEvents(DbContext? context)
    {
        // A deleted entity is no longer tracked after the save, so the raisers come from SavingChanges, not the tracker.
        if (context is null || !_saving.TryGetValue(context, out var raisers))
            return;

        _saving.Remove(context);
        foreach (var entity in raisers)
            entity.ClearDomainEvents();
    }

    private void WriteToOutbox(DbContext? context)
    {
        if (context is null)
            return;

        // A failed save left its raisers here; this save starts over.
        _saving.Remove(context);

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

        // A failed save leaves its rows in the context: a retry of it saves them, so their events aren't added again.
        var written = context.ChangeTracker.Entries<OutboxMessage>()
            .Select(entry => entry.Entity.EventId)
            .ToHashSet();

        foreach (var entity in raisers)
        {
            foreach (var domainEvent in entity.DomainEvents.Where(domainEvent => !written.Contains(domainEvent.EventId)))
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
        }

        _saving.AddOrUpdate(context, raisers);
    }
}
