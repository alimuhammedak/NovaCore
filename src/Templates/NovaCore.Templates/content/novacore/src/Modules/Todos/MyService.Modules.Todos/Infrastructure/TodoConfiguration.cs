using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyService.Modules.Todos.Domain;

namespace MyService.Modules.Todos.Infrastructure;

internal sealed class TodoConfiguration : IEntityTypeConfiguration<Todo>
{
    public void Configure(EntityTypeBuilder<Todo> builder)
    {
        builder.ToTable("items");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(256);
        builder.Property(t => t.ModifiedBy).HasMaxLength(256);

        // The list is ordered by title.
        builder.HasIndex(t => t.Title);

        // The event buffer isn't a column: saving writes the events to the module's outbox table.
        builder.Ignore(t => t.DomainEvents);
    }
}
