using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Infrastructure;
using Respawn;
using Respawn.Graph;
using Xunit;

namespace NovaCore.Api.Tests.SqlServer;

/// <summary>
/// The test host (<see cref="ApiFactory"/>) on a real SQL Server (<see cref="TestSqlServer"/>), in a database of its own.
/// The DbContexts keep the SQL Server registrations Program gives them, and the host applies the migrations when it starts,
/// as production does (<c>Database:ApplyMigrations</c>, <c>DatabaseMigrationService</c>): every migration of the three
/// contexts runs, its SQL steps included. Without Docker the host isn't built; the tests skip through
/// <see cref="Database"/>. <see cref="ResetAsync"/> empties the module tables between the tests of a class.
/// </summary>
public sealed class SqlServerApiFactory : ApiFactory, IAsyncLifetime
{
    private Respawner? _respawner;

    internal TestDatabase Database { get; private set; } = new(null, "not initialized");

    /// <summary>Creates the database and starts the host, which migrates it before the first test.</summary>
    public async ValueTask InitializeAsync()
    {
        Database = await TestSqlServer.CreateDatabaseAsync();
        if (Database.IsAvailable)
            _ = Server;
    }

    /// <summary>
    /// Deletes every row of the module schemas (Respawn), keeping their migration history; skips the test without Docker.
    /// The identity store keeps its rows: the tests here sign nobody in through it.
    /// </summary>
    public async Task ResetAsync()
    {
        Database.SkipIfUnavailable();
        await using var connection = new SqlConnection(Database.ConnectionString);
        await connection.OpenAsync();
        _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            SchemasToInclude = [CatalogDbContext.Schema, PaymentsDbContext.Schema],
            TablesToIgnore =
            [
                new Table(CatalogDbContext.Schema, "__EFMigrationsHistory"),
                new Table(PaymentsDbContext.Schema, "__EFMigrationsHistory")
            ]
        });
        await _respawner.ResetAsync(connection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:Database"] = Database.ConnectionString,
                ["Database:ApplyMigrations"] = "true"
            }));
    }

    // Program's SQL Server registrations stay.
    protected override void ConfigureDatabases(IServiceCollection services)
    {
    }
}
