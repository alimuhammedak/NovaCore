using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NovaCore.Identity.OpenIddict;

namespace NovaCore.Api.Identity;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> can build the identity provider's model offline. The context comes
/// from the NovaCore.Identity.OpenIddict package; its SQL Server migrations live in this host (docs/plan/RESEARCH.md R-060).
/// </summary>
public sealed class IdentityServerDbContextFactory : IDesignTimeDbContextFactory<IdentityServerDbContext>
{
    public IdentityServerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityServerDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=NovaCore;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;",
                sql => sql
                    .MigrationsAssembly(typeof(IdentityServerDbContextFactory).Assembly.GetName().Name)
                    .MigrationsHistoryTable("__EFMigrationsHistory", IdentityServerDbContext.Schema))
            .Options;

        return new IdentityServerDbContext(options);
    }
}
