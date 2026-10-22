using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NovaCore.Api.Tests.SqlServer;

/// <summary>
/// The test host (<see cref="ApiFactory"/>) on a real SQL Server (<see cref="TestSqlServer"/>), in a database of its own.
/// The DbContexts keep the SQL Server registrations Program gives them, and the host applies the migrations when it starts,
/// as production does (<c>Database:ApplyMigrations</c>, <c>DatabaseMigrationService</c>): every migration of the three
/// contexts runs, its SQL steps included. Without Docker the host isn't built; the tests skip through
/// <see cref="Database"/>.
/// </summary>
public sealed class SqlServerApiFactory : ApiFactory, IAsyncLifetime
{
    internal TestDatabase Database { get; private set; } = new(null, "not initialized");

    /// <summary>Creates the database and starts the host, which migrates it before the first test.</summary>
    public async ValueTask InitializeAsync()
    {
        Database = await TestSqlServer.CreateDatabaseAsync();
        if (Database.IsAvailable)
            _ = Server;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
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
