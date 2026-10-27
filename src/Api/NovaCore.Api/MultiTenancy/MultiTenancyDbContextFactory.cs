using Microsoft.EntityFrameworkCore.Design;
using NovaCore.MultiTenancy;

namespace NovaCore.Api.MultiTenancy;

/// <summary>
/// Lets <c>dotnet ef</c> build the tenancy store's model offline. The context comes from the NovaCore.MultiTenancy
/// package; its SQL Server migrations live in this host, like the identity provider's.
/// </summary>
public sealed class MultiTenancyDbContextFactory : IDesignTimeDbContextFactory<MultiTenancyDbContext>
{
    public MultiTenancyDbContext CreateDbContext(string[] args) =>
        new(DesignTimeDatabase.Options<MultiTenancyDbContext>(MultiTenancyDbContext.Schema));
}
