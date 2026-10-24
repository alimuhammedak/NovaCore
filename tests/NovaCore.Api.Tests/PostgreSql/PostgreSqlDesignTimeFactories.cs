using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using NovaCore.Identity.OpenIddict;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Infrastructure;
using NovaCore.MultiTenancy;

namespace NovaCore.Api.Tests.PostgreSql;

/// <summary>
/// What <c>dotnet ef</c> builds the contexts with for the PostgreSQL migrations in this project, as
/// <see cref="PostgreSqlApiFactory"/> and <c>AddModuleDbContext</c> configure them at run time: PostgreSQL, this
/// assembly's migrations, each context's migration history in its own schema. With this project as both the project and
/// the startup project:
/// <c>dotnet ef migrations add &lt;Name&gt; --project tests/NovaCore.Api.Tests --startup-project tests/NovaCore.Api.Tests
/// --context &lt;Context&gt; --output-dir PostgreSql/&lt;Module&gt;/Migrations</c>.
/// </summary>
internal static class PostgreSqlDesignTime
{
    public static DbContextOptions<TContext> Options<TContext>(string schema)
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(
                "Host=localhost;Database=novacore",
                npgsql => npgsql
                    .MigrationsAssembly(typeof(PostgreSqlDesignTime).Assembly)
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema))
            .Options;
}

/// <summary>The Catalog module's PostgreSQL migrations (<see cref="PostgreSqlDesignTime"/>).</summary>
public sealed class CatalogPostgreSqlFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args) =>
        new(PostgreSqlDesignTime.Options<CatalogDbContext>(CatalogDbContext.Schema));
}

/// <summary>The Payments module's PostgreSQL migrations (<see cref="PostgreSqlDesignTime"/>).</summary>
public sealed class PaymentsPostgreSqlFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args) =>
        new(PostgreSqlDesignTime.Options<PaymentsDbContext>(PaymentsDbContext.Schema));
}

/// <summary>The identity provider's PostgreSQL migrations (<see cref="PostgreSqlDesignTime"/>).</summary>
public sealed class IdentityServerPostgreSqlFactory : IDesignTimeDbContextFactory<IdentityServerDbContext>
{
    public IdentityServerDbContext CreateDbContext(string[] args) =>
        new(PostgreSqlDesignTime.Options<IdentityServerDbContext>(IdentityServerDbContext.Schema));
}

/// <summary>The tenancy store's PostgreSQL migrations (<see cref="PostgreSqlDesignTime"/>).</summary>
public sealed class MultiTenancyPostgreSqlFactory : IDesignTimeDbContextFactory<MultiTenancyDbContext>
{
    public MultiTenancyDbContext CreateDbContext(string[] args) =>
        new(PostgreSqlDesignTime.Options<MultiTenancyDbContext>(MultiTenancyDbContext.Schema));
}
