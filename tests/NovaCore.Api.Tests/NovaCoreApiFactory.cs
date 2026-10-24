using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Identity.OpenIddict;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Infrastructure;

namespace NovaCore.Api.Tests;

/// <summary>
/// The test host (<see cref="ApiFactory"/>) on in-memory SQLite: every DbContext (the modules' and the identity
/// provider's) gets its own in-memory SQLite database through <c>AddModuleDatabase</c>, so the database health checks and
/// the provider's flows run without a real SQL Server, and every context is still registered by
/// <c>AddModuleDbContext</c>, with its interceptors and tenant isolation. The module databases start with their schema,
/// as after the migrations, because the host's outbox dispatcher reads their outboxes from the start.
/// </summary>
public class NovaCoreApiFactory : ApiFactory
{
    // A named in-memory database per context, shared by every connection with its name: each DbContext opens its own
    // connection, because the outbox dispatcher reads the module databases in the background while requests use them, and
    // a SqliteConnection isn't thread-safe (docs/plan/RESEARCH.md R-069). These open connections keep the databases alive
    // for the factory's lifetime.
    private readonly SqliteConnection _identityDatabase = OpenSharedInMemoryDatabase();
    private readonly SqliteConnection _catalogDatabase = OpenSharedInMemoryDatabase();
    private readonly SqliteConnection _paymentsDatabase = OpenSharedInMemoryDatabase();

    public NovaCoreApiFactory()
    {
        var catalogOptions = new DbContextOptionsBuilder<CatalogDbContext>();
        UseSqlite(catalogOptions, _catalogDatabase.ConnectionString);
        using var catalog = new CatalogDbContext(catalogOptions.Options);
        catalog.Database.EnsureCreated();

        var paymentsOptions = new DbContextOptionsBuilder<PaymentsDbContext>();
        UseSqlite(paymentsOptions, _paymentsDatabase.ConnectionString);
        using var payments = new PaymentsDbContext(paymentsOptions.Options);
        payments.Database.EnsureCreated();
    }

    /// <summary>The Catalog database, for a subclass that writes to it on a connection of its own.</summary>
    protected string CatalogConnectionString => _catalogDatabase.ConnectionString;

    // A later AddModuleDatabase replaces Program's, so Program's SQL Server is never configured and every context has one
    // provider.
    protected override void ConfigureDatabases(IServiceCollection services) =>
        services.AddModuleDatabase((_, options) => UseDatabase(options));

    /// <summary>
    /// Gives a module context (<c>options.Options.ContextType</c>) its SQLite database. A subclass points one context
    /// elsewhere or adds an interceptor to it, and calls this for the others.
    /// </summary>
    protected virtual void UseDatabase(DbContextOptionsBuilder options)
    {
        var contextType = options.Options.ContextType;
        var database = contextType == typeof(CatalogDbContext) ? _catalogDatabase
            : contextType == typeof(PaymentsDbContext) ? _paymentsDatabase
            : contextType == typeof(IdentityServerDbContext) ? _identityDatabase
            : throw new InvalidOperationException($"The test host has no database for {contextType.Name}.");

        UseSqlite(options, database.ConnectionString);
    }

    /// <summary>
    /// SQLite at <paramref name="connectionString"/>, storing <see cref="DateTimeOffset"/> as UTC ticks, which SQLite can
    /// compare and order.
    /// </summary>
    protected static DbContextOptionsBuilder UseSqlite(DbContextOptionsBuilder options, string connectionString) =>
        options.UseSqlite(connectionString).ReplaceService<IModelCustomizer, UtcTicksModelCustomizer>();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _identityDatabase.Dispose();
            _catalogDatabase.Dispose();
            _paymentsDatabase.Dispose();
        }

        base.Dispose(disposing);
    }

    // No pooling: a pooled connection would keep the database alive after the factory closes this one.
    private static SqliteConnection OpenSharedInMemoryDatabase()
    {
        var connection = new SqliteConnection(
            $"Data Source=novacore-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False");
        connection.Open();
        return connection;
    }
}
