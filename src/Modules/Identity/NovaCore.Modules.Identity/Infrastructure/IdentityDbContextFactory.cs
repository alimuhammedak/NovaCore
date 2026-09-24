using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NovaCore.Modules.Identity.Infrastructure;

/// <summary>Design-time factory so <c>dotnet ef migrations</c> can build the Identity model offline.</summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=NovaCore;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema))
            .Options;

        return new IdentityDbContext(options);
    }
}
