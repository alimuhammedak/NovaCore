using Microsoft.EntityFrameworkCore;
using NovaCore.Modules.Catalog.Domain;

namespace NovaCore.Modules.Catalog.Infrastructure;

/// <summary>
/// The Catalog module's own DbContext, pinned to the <c>catalog</c> schema. Each module keeps a
/// separate context and schema — the modular-monolith boundary made physical at the database level.
/// </summary>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public const string Schema = "catalog";

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
