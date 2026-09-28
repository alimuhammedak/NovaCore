using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaCore.Modules.Catalog.Domain;

namespace NovaCore.Modules.Catalog.Infrastructure;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Price).HasPrecision(18, 2);

        // Stock is the concurrency token: any update or delete of a product whose stock changed after it was read fails
        // (DbUpdateConcurrencyException) instead of undoing a sale or selling the same unit twice. Concurrent changes to
        // other columns alone don't conflict. Application-managed, so it works on SQLite too (docs/plan/RESEARCH.md R-075).
        builder.Property(p => p.Stock).IsConcurrencyToken();
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.ModifiedBy).HasMaxLength(256);

        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.IsActive);

        // The event buffer isn't a column: saving writes the events to the module's outbox table (OutboxInterceptor).
        builder.Ignore(p => p.DomainEvents);
    }
}
