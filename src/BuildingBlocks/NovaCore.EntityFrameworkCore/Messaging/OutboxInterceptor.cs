using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NovaCore.Abstractions.Entities;
using NovaCore.EntityFrameworkCore.Persistence;

namespace NovaCore.EntityFrameworkCore.Messaging;

/// <summary>
/// Writes the domain events raised on a context's entities to its outbox (<see cref="OutboxModelBuilderExtensions.AddOutbox"/>)
/// as part of the SaveChanges that saves the entities, then clears the entities' buffers. EF Core runs this before it
/// detects changes and saves, so the outbox rows share the save's transaction: the events are committed with the entity
/// changes or not at all. <see cref="PersistenceExtensions.AddModuleDbContext{TContext}"/> adds it to every module context.
/// If a context's entities raise events but its model has no outbox, the save fails rather than drop them.
/// </summary>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
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
                    OccurredOnUtc = domainEvent.OccurredOnUtc
                });
            }

            entity.ClearDomainEvents();
        }
    }
}
