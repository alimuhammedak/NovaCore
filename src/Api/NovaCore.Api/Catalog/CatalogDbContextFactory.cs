using Microsoft.EntityFrameworkCore.Design;
using NovaCore.Modules.Catalog.Infrastructure;

namespace NovaCore.Api.Catalog;

/// <summary>
/// Lets <c>dotnet ef</c> build the Catalog module's model offline. The module chooses no database: its SQL Server
/// migrations live in this host, next to this factory.
/// </summary>
public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args) =>
        new(DesignTimeDatabase.Options<CatalogDbContext>(CatalogDbContext.Schema));
}
