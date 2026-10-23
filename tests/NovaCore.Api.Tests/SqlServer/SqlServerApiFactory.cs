using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Api.Tests.Databases;
using Respawn;

namespace NovaCore.Api.Tests.SqlServer;

/// <summary>
/// The test host on a real SQL Server (<see cref="TestSqlServer"/>), in a database of its own: the DbContexts keep the
/// SQL Server database Program gives them (<c>AddModuleDatabase</c>) and the host's SQL Server migrations.
/// </summary>
public class SqlServerApiFactory : DatabaseApiFactory
{
    private protected override Task<TestDatabase> CreateDatabaseAsync() => TestSqlServer.CreateDatabaseAsync();

    protected override DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    protected override IDbAdapter RespawnAdapter => DbAdapter.SqlServer;

    // Program's SQL Server database stays.
    protected override void ConfigureDatabases(IServiceCollection services)
    {
    }
}

/// <summary><see cref="SqlServerApiFactory"/> as its own authority, so the identity provider's tokens reach the modules.</summary>
public sealed class SqlServerAuthorityApiFactory : SqlServerApiFactory
{
    protected override bool IsItsOwnAuthority => true;
}
