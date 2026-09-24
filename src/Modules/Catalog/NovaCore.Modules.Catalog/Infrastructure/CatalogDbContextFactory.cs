using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NovaCore.Modules.Catalog.Infrastructure;

/// <summary>
/// Lets <c>dotnet ef migrations</c> build the model offline (no running database, no host) by
/// supplying a design-time context. The connection string here is a placeholder — migrations only
/// need the provider to shape SQL, not an actual connection.
/// </summary>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=NovaCore;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema))
            .Options;

        return new CatalogDbContext(options);
    }
}
