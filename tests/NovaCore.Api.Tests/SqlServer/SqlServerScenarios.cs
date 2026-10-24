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
