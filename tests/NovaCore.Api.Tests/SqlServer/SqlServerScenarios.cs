using Microsoft.EntityFrameworkCore;
using NovaCore.Api.Tests.Databases;
using NovaCore.Api.Tests.IdentityServer;
using Xunit;

namespace NovaCore.Api.Tests.SqlServer;

/// <summary>The outbox on SQL Server (<see cref="OutboxScenarios{TFactory}"/>).</summary>
public sealed class SqlServerOutboxTests(SqlServerApiFactory factory) : OutboxScenarios<SqlServerApiFactory>(factory);

/// <summary>The identity provider on SQL Server (<see cref="SignInScenarios{TFactory}"/>).</summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class SqlServerSignInTests(SqlServerAuthorityApiFactory factory)
    : SignInScenarios<SqlServerAuthorityApiFactory>(factory);

/// <summary>Tenant isolation on SQL Server (<see cref="TenancyScenarios{TFixture}"/>).</summary>
public sealed class SqlServerTenancyTests(SqlServerTenancyFixture fixture) : TenancyScenarios<SqlServerTenancyFixture>(fixture);

/// <summary>The tenancy host on a database of its own on the SQL Server test server.</summary>
public sealed class SqlServerTenancyFixture : TenancyFixture
{
    private protected override Task<TestDatabase> CreateDatabaseAsync() => TestSqlServer.CreateDatabaseAsync();

    protected override void UseDatabase(DbContextOptionsBuilder options, string connectionString) =>
        options.UseSqlServer(connectionString);
}
