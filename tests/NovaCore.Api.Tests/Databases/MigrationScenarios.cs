using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Identity.OpenIddict;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Infrastructure;
using NovaCore.MultiTenancy;
using Xunit;

namespace NovaCore.Api.Tests.Databases;

/// <summary>
/// The startup migrations on a real database, the same on every provider: the host's <c>DatabaseMigrationService</c>
/// applies every migration of the four contexts when it starts (their SQL runs nowhere else: the SQLite tests take their
/// schema from <c>EnsureCreated</c>), and each context keeps its tables and its migration history in its own schema.
/// </summary>
public abstract class MigrationScenarios<TFactory>(TFactory factory) : IClassFixture<TFactory>
    where TFactory : DatabaseApiFactory
{
    protected TFactory Factory { get; } = factory;

    /// <summary>The schemas the server keeps for itself, which <c>INFORMATION_SCHEMA.TABLES</c> lists too.</summary>
    protected abstract IReadOnlyCollection<string> SystemSchemas { get; }

    [Fact]
    public async Task The_host_applies_every_migration_of_every_context_when_it_starts()
    {
        Factory.Database.SkipIfUnavailable();
        await using var scope = Factory.Services.CreateAsyncScope();

        foreach (var context in Contexts(scope.ServiceProvider))
        {
            Assert.NotEmpty(context.Database.GetMigrations());
            Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        }
    }

    [Fact]
    public async Task Each_context_keeps_its_tables_and_its_migration_history_in_its_own_schema()
    {
        Factory.Database.SkipIfUnavailable();
        await using var scope = Factory.Services.CreateAsyncScope();
        var contexts = Contexts(scope.ServiceProvider);
        var sql = new PortableSql(contexts[0]);

        // Standard SQL: INFORMATION_SCHEMA is on both providers (an unquoted name folds to PostgreSQL's lower case).
        var query = $"SELECT TABLE_SCHEMA AS {sql.Column("Schema")}, TABLE_NAME AS {sql.Column("Name")} " +
                    "FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
        var tables = (await contexts[0].Database.SqlQueryRaw<TableName>(query).ToListAsync())
            .Where(table => !SystemSchemas.Contains(table.Schema, StringComparer.Ordinal))
            .ToList();

        foreach (var context in contexts)
        {
            var schema = context.Model.GetDefaultSchema()!;
            var modelTables = context.Model.GetEntityTypes()
                .Where(entity => entity.GetTableName() is not null)
                .Select(entity => new TableName(entity.GetSchema()!, entity.GetTableName()!))
                .Append(new TableName(schema, "__EFMigrationsHistory"))
                .Distinct();

            Assert.Equal(InOrder(modelTables), InOrder(tables.Where(table => string.Equals(table.Schema, schema, StringComparison.Ordinal))));
        }

        // Nothing outside the contexts' schemas (no dbo, no public).
        Assert.Equal(
            contexts.Select(context => context.Model.GetDefaultSchema()).Order(StringComparer.Ordinal),
            tables.Select(table => table.Schema).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
    }

    private static DbContext[] Contexts(IServiceProvider services) =>
    [
        services.GetRequiredService<CatalogDbContext>(),
        services.GetRequiredService<PaymentsDbContext>(),
        services.GetRequiredService<IdentityServerDbContext>(),
        services.GetRequiredService<MultiTenancyDbContext>()
    ];

    private static List<TableName> InOrder(IEnumerable<TableName> tables) =>
        tables.OrderBy(table => table.Schema, StringComparer.Ordinal).ThenBy(table => table.Name, StringComparer.Ordinal).ToList();

    private sealed record TableName(string Schema, string Name);
}
