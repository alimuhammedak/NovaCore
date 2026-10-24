using Microsoft.EntityFrameworkCore;

namespace NovaCore.MultiTenancy;

/// <summary>
/// The tenancy store: the tenants and who is a member of which, in the <see cref="Schema"/> schema. It carries no
/// migrations: the host registers it with its database (<c>AddModuleDbContext</c>) and keeps the migrations, like the
/// identity provider's store. Its rows aren't a tenant's data: a membership names the tenant it grants, and it is read
/// before the request has a tenant.
/// </summary>
/// <param name="options">The options the host configured.</param>
public sealed class MultiTenancyDbContext(DbContextOptions<MultiTenancyDbContext> options) : DbContext(options)
{
    /// <summary>The database schema of every table, also the place of the migration history table.</summary>
    public const string Schema = "tenancy";

    internal DbSet<Tenant> Tenants => Set<Tenant>();

    internal DbSet<TenantMembership> Memberships => Set<TenantMembership>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Tenant>(tenant =>
        {
            tenant.ToTable("tenants");
            tenant.HasKey(t => t.Id);
            tenant.Property(t => t.Name).HasMaxLength(Tenant.MaxNameLength);
        });

        modelBuilder.Entity<TenantMembership>(membership =>
        {
            membership.ToTable("memberships");
            membership.HasKey(m => new { m.TenantId, m.Subject });
            membership.Property(m => m.Subject).HasMaxLength(TenantMembership.MaxSubjectLength);
            // "The tenants of a subject" reads by subject.
            membership.HasIndex(m => m.Subject);
            membership.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

/// <summary>A tenant: an organization the product is sold to.</summary>
internal sealed class Tenant
{
    public const int MaxNameLength = 200;

    public Guid Id { get; init; }

    public required string Name { get; set; }

    public TenantStatus Status { get; set; }

    /// <summary>When <see cref="Status"/> last changed (the tenant's creation, first).</summary>
    public DateTimeOffset StatusChangedOnUtc { get; set; }
}

/// <summary>A subject's membership of a tenant, with the roles it holds there.</summary>
internal sealed class TenantMembership
{
    /// <summary>The longest subject kept: the identity provider's, a Guid, or a client id.</summary>
    public const int MaxSubjectLength = 256;

    public Guid TenantId { get; init; }

    /// <summary>The member's subject: the access token's <c>sub</c>.</summary>
    public required string Subject { get; init; }

    public List<string> Roles { get; set; } = [];
}
