using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NovaCore.EntityFrameworkCore.Messaging;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Identity.OpenIddict;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Infrastructure;

namespace NovaCore.Api.Tests;

/// <summary>
/// The test host (<see cref="ApiFactory"/>) on in-memory SQLite: every DbContext (the modules' and the identity
/// provider's) is swapped to its own in-memory SQLite database, so the database health checks and the provider's flows
/// run without a real SQL Server. The module databases start with their schema, as after the migrations, because the
/// host's outbox dispatcher reads their outboxes from the start.
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

    // The SQLite contexts get their own EF internal provider: the app still registers SQL Server, and EF forbids two
    // providers in one container. It stores DateTimeOffset as UTC ticks, which SQLite can compare and order.
    private readonly ServiceProvider _sqliteServices = new ServiceCollection()
        .AddEntityFrameworkSqlite()
        .Replace(ServiceDescriptor.Singleton<IModelCustomizer, UtcTicksModelCustomizer>())
        .BuildServiceProvider();

    public NovaCoreApiFactory()
    {
        using var catalog = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_catalogDatabase).UseInternalServiceProvider(_sqliteServices).Options);
        catalog.Database.EnsureCreated();

        using var payments = new PaymentsDbContext(new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseSqlite(_paymentsDatabase).UseInternalServiceProvider(_sqliteServices).Options);
        payments.Database.EnsureCreated();
    }

    /// <summary>The Catalog database, for a subclass that registers the Catalog context again (<c>ReplaceWithSqlite</c>).</summary>
    protected string CatalogConnectionString => _catalogDatabase.ConnectionString;

    protected override void ConfigureDatabases(IServiceCollection services)
    {
        ReplaceWithSqlite<IdentityServerDbContext>(services, options => options.UseSqlite(_identityDatabase.ConnectionString));
        ReplaceWithSqlite<CatalogDbContext>(services, options => options.UseSqlite(_catalogDatabase.ConnectionString));
        ReplaceWithSqlite<PaymentsDbContext>(services, options => options.UseSqlite(_paymentsDatabase.ConnectionString));
    }

    /// <summary>
    /// Replaces a module's SQL Server registration of <typeparamref name="TContext"/> with SQLite, keeping the interceptors
    /// of the original registration: the audit interceptor the module adds and the outbox interceptor
    /// <c>AddModuleDbContext</c> adds. EF Core 10 registers the provider via additive
    /// <c>IDbContextOptionsConfiguration&lt;TContext&gt;</c>, so EVERY registration generic over the context (options,
    /// configuration, the context) is removed first.
    /// </summary>
    protected void ReplaceWithSqlite<TContext>(IServiceCollection services, Action<DbContextOptionsBuilder> useSqlite)
        where TContext : DbContext
    {
        var toRemove = services
            .Where(d => d.ServiceType == typeof(TContext)
                        || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(TContext))))
            .ToList();
        foreach (var descriptor in toRemove)
            services.Remove(descriptor);

        services.AddDbContext<TContext>((provider, options) =>
        {
            useSqlite(options);
            options
                .UseInternalServiceProvider(_sqliteServices)
                .AddInterceptors(
                    provider.GetRequiredService<AuditableEntityInterceptor>(),
                    provider.GetRequiredService<OutboxInterceptor>());
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _identityDatabase.Dispose();
            _catalogDatabase.Dispose();
            _paymentsDatabase.Dispose();
            _sqliteServices.Dispose();
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
