using Microsoft.EntityFrameworkCore.Design;
using NovaCore.Identity.OpenIddict;

namespace NovaCore.Api.Identity;

/// <summary>
/// Lets <c>dotnet ef</c> build the identity provider's model offline. The context comes from the
/// NovaCore.Identity.OpenIddict package; its SQL Server migrations live in this host (docs/plan/RESEARCH.md R-060).
/// </summary>
public sealed class IdentityServerDbContextFactory : IDesignTimeDbContextFactory<IdentityServerDbContext>
{
    public IdentityServerDbContext CreateDbContext(string[] args) =>
        new(DesignTimeDatabase.Options<IdentityServerDbContext>(IdentityServerDbContext.Schema));
}
