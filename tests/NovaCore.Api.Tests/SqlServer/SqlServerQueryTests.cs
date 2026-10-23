using NovaCore.Api.Tests.Databases;
using NovaCore.Modules.Catalog.Authorization;
using Xunit;

namespace NovaCore.Api.Tests.SqlServer;

/// <summary>The queries on SQL Server (<see cref="QueryScenarios{TFactory}"/>), and its collation.</summary>
public sealed class SqlServerQueryTests(SqlServerApiFactory factory) : QueryScenarios<SqlServerApiFactory>(factory)
{
    // EF.Functions.Like follows the column's collation: SQL Server's default ignores case but not accents (a difference of
    // the provider, not of the module: PostgreSqlQueryTests).
    [Fact]
    public async Task Product_search_ignores_case_but_not_accents_on_the_servers_default_collation()
    {
        await AddProductsAsync("Blue Widget", "BLUE gadget", "Red widget", "Café crème");
        using var client = Factory.CreateClientWithPermissions(CatalogPermissions.ProductsRead);

        Assert.Equal(["BLUE gadget", "Blue Widget"], await SearchAsync(client, "blue"), StringComparer.Ordinal);
        Assert.Equal(["Café crème"], await SearchAsync(client, "CAFÉ"), StringComparer.Ordinal);
        Assert.Empty(await SearchAsync(client, "cafe"));
    }
}
