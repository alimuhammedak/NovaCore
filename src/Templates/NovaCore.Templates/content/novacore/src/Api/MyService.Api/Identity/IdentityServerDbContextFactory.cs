using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NovaCore.Identity.OpenIddict;

namespace MyService.Api.Identity;

/// <summary>
/// Lets <c>dotnet ef</c> build the identity provider's model without starting the host. The context comes from the
/// NovaCore.Identity.OpenIddict package; its SQL Server migrations live in this project. The connection string is only a
/// placeholder: creating a migration needs the provider, not a database.
/// </summary>
public sealed class IdentityServerDbContextFactory : IDesignTimeDbContextFactory<IdentityServerDbContext>
{
    public IdentityServerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityServerDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=MyService;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;",
                sql => sql
                    .MigrationsAssembly(typeof(IdentityServerDbContextFactory).Assembly.GetName().Name)
                    .MigrationsHistoryTable("__EFMigrationsHistory", IdentityServerDbContext.Schema))
            .Options;

        return new IdentityServerDbContext(options);
    }
}
