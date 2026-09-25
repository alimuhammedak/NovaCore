using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NovaCore.Abstractions.Messaging;
using NovaCore.EntityFrameworkCore.Messaging;

namespace NovaCore.EntityFrameworkCore.Persistence;

/// <summary>Records that a module owns a given DbContext, so the host can migrate them all generically.</summary>
public sealed record ModuleDbContextDescriptor(Type ContextType);

/// <summary>Service registration for module-owned DbContexts.</summary>
public static class PersistenceExtensions
{
    // The readiness tag, as in the health-check guidance (R-049): /health includes these checks.
    private const string ReadyTag = "ready";

    /// <summary>
    /// Registers the persistence services every module DbContext relies on: the audit-stamping interceptor, the
    /// in-process domain-event dispatcher and the startup migrations, which run only when
    /// <c>Database:ApplyMigrations</c> is true. Call it before registering the modules, so the migrations run before any
    /// module's own hosted services.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static IHostApplicationBuilder AddCorePersistence(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Audit stamping runs inside the unit of work; scoped so it sees the request's ICurrentUser.
        services.TryAddScoped<AuditableEntityInterceptor>();
        services.TryAddSingleton(TimeProvider.System);

        // In-process domain-event dispatch — no commercial mediator required.
        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

        // Opt-in: apply every module's EF Core migrations on startup. Off by default so test hosts and DB-less runs
        // never block. Enable with Database:ApplyMigrations=true once SQL Server is reachable. The service is always
        // registered and reads the flag when the host starts; ValidateOnStart surfaces a value that isn't a bool.
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateOnStart();
        services.AddHostedService<DatabaseMigrationService>();

        return builder;
    }

    /// <summary>
    /// Registers a module's DbContext, advertises it for generic startup migration and adds a health check (named after
    /// the context and tagged <c>ready</c>) that reports whether its database can be reached.
    /// Each module keeps its own context (and its own schema) — the modular-monolith boundary at the DB level.
    /// Also registers <see cref="AuditableEntityInterceptor"/> (and the <see cref="TimeProvider"/> it stamps with), which
    /// module contexts add to their options, so the context resolves whether or not the host has called
    /// <see cref="AddCorePersistence"/> yet.
    /// </summary>
    /// <typeparam name="TContext">The module's DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the context options (provider, connection string, interceptors).</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configure)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(configure);
        services.AddSingleton(new ModuleDbContextDescriptor(typeof(TContext)));
        services.TryAddScoped<AuditableEntityInterceptor>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddHealthChecks().AddDbContextCheck<TContext>(tags: [ReadyTag]);
        return services;
    }
}
