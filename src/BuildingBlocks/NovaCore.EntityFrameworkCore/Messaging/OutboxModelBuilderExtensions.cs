using Microsoft.EntityFrameworkCore;

namespace NovaCore.EntityFrameworkCore.Messaging;

/// <summary>Adds the transactional outbox to a module's model.</summary>
public static class OutboxModelBuilderExtensions
{
    /// <summary>
    /// Maps the outbox (the <c>outbox_messages</c> table) in the context's default schema. <see cref="OutboxInterceptor"/>
    /// writes the domain events raised on the context's entities there, in the save that saves the entities. Call it from
    /// <c>OnModelCreating</c> of every module DbContext, then add a migration.
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
            message.Property(m => m.Type).HasMaxLength(512);
        });

        return modelBuilder;
    }
}
