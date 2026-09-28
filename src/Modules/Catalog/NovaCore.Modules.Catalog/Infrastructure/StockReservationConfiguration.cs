using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaCore.Modules.Catalog.Domain;

namespace NovaCore.Modules.Catalog.Infrastructure;

internal sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");

        // Keyed by the payment: one answer per payment, however often its request is delivered. No foreign key to the
        // product: the answer outlives a deleted product, so a late redelivery still finds it.
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(r => r.Reason).HasMaxLength(100);
        builder.Property(r => r.CreatedBy).HasMaxLength(256);
        builder.Property(r => r.ModifiedBy).HasMaxLength(256);

        builder.Ignore(r => r.DomainEvents);
    }
}
