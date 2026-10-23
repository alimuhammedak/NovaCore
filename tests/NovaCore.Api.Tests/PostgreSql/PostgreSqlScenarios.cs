using NovaCore.Api.Tests.Databases;
using NovaCore.Api.Tests.IdentityServer;
using NovaCore.Modules.Catalog.Authorization;
using Xunit;

namespace NovaCore.Api.Tests.PostgreSql;

/// <summary>The startup migrations on PostgreSQL (<see cref="MigrationScenarios{TFactory}"/>).</summary>
public sealed class PostgreSqlMigrationTests(PostgreSqlApiFactory factory) : MigrationScenarios<PostgreSqlApiFactory>(factory)
{
    protected override IReadOnlyCollection<string> SystemSchemas { get; } = ["pg_catalog", "information_schema"];
}

/// <summary>The outbox on PostgreSQL (<see cref="OutboxScenarios{TFactory}"/>).</summary>
public sealed class PostgreSqlOutboxTests(PostgreSqlApiFactory factory) : OutboxScenarios<PostgreSqlApiFactory>(factory);

/// <summary>The queries on PostgreSQL (<see cref="QueryScenarios{TFactory}"/>), and its collation.</summary>
public sealed class PostgreSqlQueryTests(PostgreSqlApiFactory factory) : QueryScenarios<PostgreSqlApiFactory>(factory)
{
    // PostgreSQL's default collation is deterministic, so LIKE tells cases apart, unlike SQL Server's default (a
    // difference of the provider, not of the module: SqlServerQueryTests).
    [Fact]
    public async Task Product_search_tells_cases_apart_on_the_servers_default_collation()
    {
        await AddProductsAsync("Blue Widget", "BLUE gadget", "Red widget");
        using var client = Factory.CreateClientWithPermissions(CatalogPermissions.ProductsRead);

        Assert.Equal(["Blue Widget"], await SearchAsync(client, "Blue"), StringComparer.Ordinal);
        Assert.Empty(await SearchAsync(client, "blue"));
    }
}

/// <summary>The identity provider on PostgreSQL (<see cref="SignInScenarios{TFactory}"/>).</summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class PostgreSqlSignInTests(PostgreSqlAuthorityApiFactory factory)
    : SignInScenarios<PostgreSqlAuthorityApiFactory>(factory);
