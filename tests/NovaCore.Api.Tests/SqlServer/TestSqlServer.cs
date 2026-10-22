using System.Diagnostics.CodeAnalysis;
using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace NovaCore.Api.Tests.SqlServer;

/// <summary>
/// One SQL Server for the whole test run, in a container (Testcontainers), started by the first test that needs it. Each
/// test class works in a database of its own on it (<see cref="CreateDatabaseAsync"/>), so the classes run in parallel.
/// Without Docker the SQL Server tests skip; CI runs them with <c>--fail-skips on</c>, so there a skip fails the run
/// (docs/plan/DECISIONS.md D-056).
/// </summary>
internal static class TestSqlServer
{
    /// <summary>The image docker-compose.yml runs, digest included (<c>SqlServerImageTests</c> keeps the two equal).</summary>
    public const string Image =
        "mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04@sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090";

    // xUnit fixtures can't depend on one another, so the class fixtures share the container through this field. It is
    // never disposed here: Testcontainers' resource reaper removes it when the test process ends.
    private static readonly Lazy<Task<Instance>> Shared = new(StartAsync);

    /// <summary>A new database on the shared server (the first migration creates it), or why there is no server.</summary>
    public static async Task<TestDatabase> CreateDatabaseAsync()
    {
        var server = await Shared.Value;
        return server.Container is null
            ? new TestDatabase(null, server.Unavailable)
            : new TestDatabase(
                new SqlConnectionStringBuilder(server.Container.GetConnectionString())
                {
                    InitialCatalog = $"novacore_{Guid.NewGuid():N}"
                }.ConnectionString,
                null);
    }

    private static async Task<Instance> StartAsync()
    {
        try
        {
            var container = new MsSqlBuilder(Image).Build();
            await container.StartAsync();
            return new Instance(container, null);
        }
        catch (DockerUnavailableException exception)
        {
            return new Instance(null, exception.Message);
        }
    }

    private sealed record Instance(MsSqlContainer? Container, string? Unavailable);
}

/// <summary>A database on the test SQL Server, or why there is none (no Docker).</summary>
internal sealed record TestDatabase(string? ConnectionString, string? Unavailable)
{
    [MemberNotNullWhen(true, nameof(ConnectionString))]
    public bool IsAvailable => ConnectionString is not null;

    /// <summary>Skips the calling test when there is no database, naming why.</summary>
    [MemberNotNull(nameof(ConnectionString))]
    public void SkipIfUnavailable()
    {
        Assert.SkipUnless(IsAvailable, $"The SQL Server tests need Docker: {Unavailable}");
    }
}
