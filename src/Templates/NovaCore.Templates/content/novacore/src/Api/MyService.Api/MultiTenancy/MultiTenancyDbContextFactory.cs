using Microsoft.EntityFrameworkCore.Design;
using NovaCore.MultiTenancy;

namespace MyService.Api.MultiTenancy;

/// <summary>
/// Lets <c>dotnet ef</c> build the tenant store's model without starting the host. The context comes from the
/// NovaCore.MultiTenancy package; its migrations live in this project.
/// </summary>
public sealed class MultiTenancyDbContextFactory : IDesignTimeDbContextFactory<MultiTenancyDbContext>
{
    public MultiTenancyDbContext CreateDbContext(string[] args) =>
        new(DesignTimeDatabase.Options<MultiTenancyDbContext>(MultiTenancyDbContext.Schema));
}
