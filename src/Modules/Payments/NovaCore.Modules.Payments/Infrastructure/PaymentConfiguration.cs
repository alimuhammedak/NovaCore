using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaCore.Modules.Payments.Domain;

namespace NovaCore.Modules.Payments.Infrastructure;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.UnitPrice).HasPrecision(18, 2);
        builder.Property(p => p.Amount).HasPrecision(18, 2);

        // Persist the status as readable text rather than an opaque integer.
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.ModifiedBy).HasMaxLength(256);

        builder.HasIndex(p => p.ProductId);
        builder.HasIndex(p => p.Status);

        builder.Ignore(p => p.DomainEvents);
    }
}
