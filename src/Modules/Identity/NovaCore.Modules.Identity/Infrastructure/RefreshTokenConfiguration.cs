using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NovaCore.Modules.Identity.Domain;

namespace NovaCore.Modules.Identity.Infrastructure;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);
        builder.Property(t => t.CreatedByIp).HasMaxLength(64);
        builder.Property(t => t.RevokedByIp).HasMaxLength(64);
        builder.Property(t => t.ReasonRevoked).HasMaxLength(64);

        // Lookups happen by hash; a token family is revoked en masse on reuse detection.
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.Family);
        builder.HasIndex(t => t.UserId);
    }
}
