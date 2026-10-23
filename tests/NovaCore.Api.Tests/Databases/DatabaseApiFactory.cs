using System.Data.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Api.Tests.IdentityServer;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Infrastructure;
using Respawn;
using Respawn.Graph;
using Xunit;

namespace NovaCore.Api.Tests.Databases;

/// <summary>
/// The test host (<see cref="ApiFactory"/>) on a real database server in a container, in a database of its own: SQL
/// Server (<c>SqlServerApiFactory</c>) or PostgreSQL (<c>PostgreSqlApiFactory</c>). The host applies the migrations when it
/// starts, as production does (<c>Database:ApplyMigrations</c>, <c>DatabaseMigrationService</c>): every migration of the
/// three contexts runs, its SQL steps included. Without Docker the host isn't built; the tests skip through
/// <see cref="Database"/>. <see cref="ResetAsync"/> empties the module tables between the tests of a class.
/// </summary>
public abstract class DatabaseApiFactory : ApiFactory, IAsyncLifetime
{
    private Respawner? _respawner;

    internal TestDatabase Database { get; private set; } = new("a database", null, "not initialized");

    /// <summary>
    /// Whether the host is its own authority (<c>Jwt:Authority</c> = its own address, JwtBearer's backchannel to its own
    /// server), so the identity provider's tokens reach the modules, as in production.
    /// </summary>
    protected virtual bool IsItsOwnAuthority => false;

    /// <summary>Creates the database and starts the host, which migrates it before the first test.</summary>
    public async ValueTask InitializeAsync()
    {
        Database = await CreateDatabaseAsync();
        if (Database.IsAvailable)
            _ = Server;
    }

    /// <summary>
    /// Deletes every row of the module schemas (Respawn), keeping their migration history; skips the test without Docker.
    /// The identity store keeps its rows: the tests here that sign in use accounts of their own.
    /// </summary>
    public async Task ResetAsync()
    {
        Database.SkipIfUnavailable();
        await using var connection = CreateConnection(Database.ConnectionString);
        await connection.OpenAsync();
        _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = RespawnAdapter,
            SchemasToInclude = [CatalogDbContext.Schema, PaymentsDbContext.Schema],
            TablesToIgnore =
            [
                new Table(CatalogDbContext.Schema, "__EFMigrationsHistory"),
                new Table(PaymentsDbContext.Schema, "__EFMigrationsHistory")
            ]
        });
        await _respawner.ResetAsync(connection);
    }

    /// <summary>A database of its own on the provider's test server.</summary>
    private protected abstract Task<TestDatabase> CreateDatabaseAsync();

    /// <summary>A connection of the provider's ADO.NET driver.</summary>
    protected abstract DbConnection CreateConnection(string connectionString);

    /// <summary>How Respawn reads the provider's schema.</summary>
    protected abstract IDbAdapter RespawnAdapter { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:Database"] = Database.ConnectionString,
                ["Database:ApplyMigrations"] = "true"
            }));

        if (IsItsOwnAuthority)
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) { ["Jwt:Authority"] = IdentityServerTestHost.Authority }));
            builder.ConfigureServices(services => services.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options => options.BackchannelHttpHandler = new IdentityServerTestHost.TestServerHandler(() => Server)));
        }
    }
}
