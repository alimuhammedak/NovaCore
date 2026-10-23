using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Api.Tests.Databases;
using NovaCore.EntityFrameworkCore.Persistence;
using Npgsql;
using Respawn;

namespace NovaCore.Api.Tests.PostgreSql;

/// <summary>
/// The test host on a real PostgreSQL (<see cref="TestPostgreSql"/>), in a database of its own. It gives every module
/// context PostgreSQL in place of Program's SQL Server (a later <c>AddModuleDatabase</c> replaces the earlier one), with
/// the PostgreSQL migrations of this assembly (<c>PostgreSql/&lt;Module&gt;/Migrations</c>): a provider's migrations live
/// in an assembly of their own, the host's for SQL Server (docs/plan/DECISIONS.md D-073, D-074).
/// </summary>
public class PostgreSqlApiFactory : DatabaseApiFactory
{
    private protected override Task<TestDatabase> CreateDatabaseAsync() => TestPostgreSql.CreateDatabaseAsync();

    protected override DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    protected override IDbAdapter RespawnAdapter => DbAdapter.Postgres;

    protected override void ConfigureDatabases(IServiceCollection services) =>
        services.AddModuleDatabase((_, options) => options.UseNpgsql(
            Database.ConnectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(PostgreSqlApiFactory).Assembly)));
}

/// <summary><see cref="PostgreSqlApiFactory"/> as its own authority, so the identity provider's tokens reach the modules.</summary>
public sealed class PostgreSqlAuthorityApiFactory : PostgreSqlApiFactory
{
    protected override bool IsItsOwnAuthority => true;
}
