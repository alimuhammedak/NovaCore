using Microsoft.EntityFrameworkCore.Design;
using NovaCore.Identity.OpenIddict;

namespace MyService.Api.Identity;

/// <summary>
/// Lets <c>dotnet ef</c> build the identity provider's model without starting the host. The context comes from the
/// NovaCore.Identity.OpenIddict package; its migrations live in this project.
/// </summary>
public sealed class IdentityServerDbContextFactory : IDesignTimeDbContextFactory<IdentityServerDbContext>
{
    public IdentityServerDbContext CreateDbContext(string[] args) =>
        new(DesignTimeDatabase.Options<IdentityServerDbContext>(IdentityServerDbContext.Schema));
}
