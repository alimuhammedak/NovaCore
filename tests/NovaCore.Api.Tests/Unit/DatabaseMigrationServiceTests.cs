using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NovaCore.EntityFrameworkCore.Persistence;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// Startup migrations follow <c>Database:ApplyMigrations</c> as the host sees it when it starts, including configuration
/// added after <c>AddCorePersistence</c> (docs/plan/RESEARCH.md R-048).
/// </summary>
public class DatabaseMigrationServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Startup_migrations_follow_the_ApplyMigrations_setting(bool applyMigrations)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var builder = Host.CreateApplicationBuilder();
        builder.AddCorePersistence();
        builder.Services.AddModuleDbContext<SampleDbContext>((_, options) => options.UseSqlite(connection));
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Database:ApplyMigrations"] = applyMigrations ? "true" : "false"
        });

        using var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        await host.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(applyMigrations, HistoryTableExists(connection));
    }

    private static bool HistoryTableExists(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory'";
        return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options);
}
