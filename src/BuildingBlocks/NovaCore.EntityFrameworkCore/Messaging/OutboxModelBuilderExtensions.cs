using Microsoft.EntityFrameworkCore;

namespace NovaCore.EntityFrameworkCore.Messaging;

/// <summary>Adds the transactional outbox to a module's model.</summary>
public static class OutboxModelBuilderExtensions
{
    /// <summary>
    /// Maps the outbox in the context's default schema: <c>outbox_messages</c>, where <see cref="OutboxInterceptor"/>
    /// writes the domain events raised on the context's entities in the save that saves the entities, and
    /// <c>outbox_consumers</c>, where the outbox dispatcher records the handlers that have handled each message (deleted
    /// with the message). Call it from <c>OnModelCreating</c> of every module DbContext, then add a migration.
    /// </summary>
    /// <param name="modelBuilder">The module's model builder.</param>
    /// <returns><paramref name="modelBuilder"/>, for chaining.</returns>
    public static ModelBuilder AddOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(message =>
        {
            message.ToTable("outbox_messages");
            message.HasKey(m => m.Id);
            message.HasIndex(m => m.EventId).IsUnique();
            // The dispatcher reads the waiting messages in Id order.
            message.HasIndex(m => new { m.ProcessedOnUtc, m.Id });
            message.Property(m => m.Type).HasMaxLength(512);
            message.Property(m => m.Error).HasMaxLength(OutboxDispatcher.MaxErrorLength);
        });

        modelBuilder.Entity<OutboxConsumer>(consumer =>
        {
            consumer.ToTable("outbox_consumers");
            consumer.HasKey(c => new { c.MessageId, c.Handler });
            consumer.Property(c => c.Handler).HasMaxLength(OutboxDispatcher.MaxHandlerLength);
            // The dispatcher deletes expired messages in bulk; the database takes their records along.
            consumer.HasOne<OutboxMessage>().WithMany().HasForeignKey(c => c.MessageId).OnDelete(DeleteBehavior.Cascade);
        });

        return modelBuilder;
    }
}
