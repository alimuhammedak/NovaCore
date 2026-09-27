using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// The identity provider's store: ASP.NET Core Identity's users, roles and claims, and OpenIddict's applications,
/// authorizations, scopes and tokens, all in the <see cref="Schema"/> schema. It carries no migrations: the host
/// registers it with its database provider (<c>AddModuleDbContext</c>) and keeps the migrations in its own assembly
/// (<c>MigrationsAssembly</c>).
/// </summary>
/// <param name="options">The options the host configured.</param>
public sealed class IdentityServerDbContext(DbContextOptions<IdentityServerDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    /// <summary>The database schema of every table, also the right place for the migrations history table.</summary>
    public const string Schema = "identity";

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity configures its entities first; the schema and OpenIddict's entities go on top.
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(Schema);
        builder.UseOpenIddict();
    }
}
