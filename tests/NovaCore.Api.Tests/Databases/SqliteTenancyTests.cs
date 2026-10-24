using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace NovaCore.Api.Tests.Databases;

/// <summary>Tenant isolation on SQLite (<see cref="TenancyScenarios{TFixture}"/>), which runs without Docker.</summary>
public sealed class SqliteTenancyTests(SqliteTenancyFixture fixture) : TenancyScenarios<SqliteTenancyFixture>(fixture);

/// <summary>
/// The tenancy host on a named, shared in-memory SQLite database. Every context opens its own connection to it; this one
/// keeps it alive as long as the fixture.
/// </summary>
public sealed class SqliteTenancyFixture : TenancyFixture
{
    private readonly SqliteConnection _database =
        new($"Data Source=novacore-tenancy-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False");

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    private protected override async Task<TestDatabase> CreateDatabaseAsync()
    {
        await _database.OpenAsync();
        return new TestDatabase("SQLite", _database.ConnectionString, null);
    }

    // DateTimeOffset as UTC ticks, which SQLite compares: the outbox dispatcher deletes processed messages by their age.
    protected override void UseDatabase(DbContextOptionsBuilder options, string connectionString) =>
        options.UseSqlite(connectionString).ReplaceService<IModelCustomizer, UtcTicksModelCustomizer>();
}
