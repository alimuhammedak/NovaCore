using DotNet.Testcontainers.Builders;
using NovaCore.Api.Tests.Databases;
using Npgsql;
using Testcontainers.PostgreSql;

namespace NovaCore.Api.Tests.PostgreSql;

/// <summary>
/// One PostgreSQL for the whole test run, in a container, like <c>TestSqlServer</c>: started by the first test that needs
/// it, a database of its own for each test class, skipped without Docker (CI runs with <c>--fail-skips on</c>).
/// </summary>
internal static class TestPostgreSql
{
    /// <summary>The image docker-compose.yml runs, digest included (<c>DatabaseImageTests</c> keeps the two equal).</summary>
    public const string Image =
        "postgres:18.4-trixie@sha256:a02db8cac496f15b094798a38254f14d6e00741f709360e5e00bb6668ea31636";

    private const string Provider = "PostgreSQL";

    private static readonly Lazy<Task<Instance>> Shared = new(StartAsync);

    /// <summary>A new database on the shared server (the first migration creates it), or why there is no server.</summary>
    public static async Task<TestDatabase> CreateDatabaseAsync()
    {
        var server = await Shared.Value;
        return server.Container is null
            ? new TestDatabase(Provider, null, server.Unavailable)
            : new TestDatabase(
                Provider,
                new NpgsqlConnectionStringBuilder(server.Container.GetConnectionString())
                {
                    Database = $"novacore_{Guid.NewGuid():N}"
                }.ConnectionString,
                null);
    }

    private static async Task<Instance> StartAsync()
    {
        try
        {
            var container = new PostgreSqlBuilder(Image).Build();
            await container.StartAsync();
            return new Instance(container, null);
        }
        catch (DockerUnavailableException exception)
        {
            return new Instance(null, exception.Message);
        }
    }

    private sealed record Instance(PostgreSqlContainer? Container, string? Unavailable);
}
