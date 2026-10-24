using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
    /// Registers the persistence services every module DbContext relies on: the audit-stamping interceptor, the startup
    /// migrations, which run only when <c>Database:ApplyMigrations</c> is true, and the outbox dispatcher, a background
    /// service that delivers the events in every module's outbox to their handlers and deletes processed messages after
    /// a retention. Call it before registering the modules, so the migrations run before any module's own hosted services.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static IHostApplicationBuilder AddCorePersistence(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Audit stamping runs inside the unit of work; scoped so it sees the request's ICurrentUser.
        services.TryAddScoped<AuditableEntityInterceptor>();
        services.TryAddSingleton(TimeProvider.System);

        // How the outbox dispatcher finds and runs the handlers of an event type, built once per type.
        services.TryAddSingleton<DomainEventHandlerInvokers>();

        // Opt-in: apply every module's EF Core migrations on startup. Off by default so test hosts and DB-less runs
        // never block. Enable with Database:ApplyMigrations=true once SQL Server is reachable. The service is always
        // registered and reads the flag when the host starts; ValidateOnStart surfaces a value that isn't a bool.
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateOnStart();
        services.AddHostedService<DatabaseMigrationService>();

        // Delivers the events in the module outboxes. Registered after the migrations, so it starts after them.
        services.AddHostedService<OutboxDispatcher>();

        return builder;
    }

    /// <summary>
    /// Gives every module DbContext (each one <see cref="AddModuleDbContext{TContext}"/> registers) its database: the
    /// provider, the connection and the provider's own options, such as the assembly that holds the provider's
    /// migrations and the retry strategy. The host chooses the database here, once; no module chooses one, so a project
    /// changes its database in one place. A later call replaces the earlier one: a test host points every module
    /// context at its own database this way. The host doesn't start without it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// Configures a module context's provider, e.g. <c>options.UseSqlServer(connectionString, sql =&gt; ...)</c>.
    /// <c>options.Options.ContextType</c> tells which context it configures.
    /// </param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddModuleDatabase(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure<ModuleDatabaseOptions>(options => options.Configure = configure);
        return services;
    }

    /// <summary>
    /// Registers a module's DbContext, advertises it for generic startup migration and adds a health check (named after
    /// the context and tagged <c>ready</c>) that reports whether its database can be reached.
    /// Each module keeps its own context (and its own schema) — the modular-monolith boundary at the DB level.
    /// The context gets the host's database (<see cref="AddModuleDatabase"/>) and keeps its migration history
    /// (<c>__EFMigrationsHistory</c>) in <paramref name="schema"/>, so each module's migrations apply on their own.
    /// It always gets <see cref="OutboxInterceptor"/>, which writes the domain events its entities raise to its
    /// outbox; the module maps that with <see cref="OutboxModelBuilderExtensions.AddOutbox"/>. The dispatcher that
    /// delivers them comes from <see cref="AddCorePersistence"/>: without it, events are written and never delivered.
    /// Also registers <see cref="AuditableEntityInterceptor"/> (and the <see cref="TimeProvider"/> it stamps with), which
    /// module contexts add to their options, so the context resolves whether or not the host has called
    /// <see cref="AddCorePersistence"/> yet.
    /// </summary>
    /// <typeparam name="TContext">The module's DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="schema">The module's schema, the one its model sets with <c>HasDefaultSchema</c>.</param>
    /// <param name="configure">
    /// Configures the module's own context options, such as its interceptors, after the host's database. Not the
    /// provider: the host chooses that.
    /// </param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        string schema,
        Action<IServiceProvider, DbContextOptionsBuilder> configure)
        where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        ArgumentNullException.ThrowIfNull(configure);

        // The host's database is checked when it starts, not on the first query.
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<ModuleDatabaseOptions>, ModuleDatabaseOptionsValidator>());
        services.AddOptions<ModuleDatabaseOptions>().ValidateOnStart();

        // The history table and the outbox interceptor are added here rather than by each module: a module that forgot
        // the first would share one history table with the others, and one that forgot the second would lose its events,
        // without an error.
        services.AddDbContext<TContext>((provider, options) =>
        {
            provider.GetRequiredService<IOptions<ModuleDatabaseOptions>>().Value.Configure!(provider, options);
            KeepMigrationHistoryIn(options, schema);
            configure(provider, options);
            options.AddInterceptors(provider.GetRequiredService<OutboxInterceptor>());
        });
        services.AddSingleton(new ModuleDbContextDescriptor(typeof(TContext)));
        services.TryAddSingleton<OutboxInterceptor>();
        services.TryAddScoped<AuditableEntityInterceptor>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddHealthChecks().AddDbContextCheck<TContext>(tags: [ReadyTag]);
        return services;
    }

    // The provider's own builder (MigrationsHistoryTable) is out of reach here, as the host chose the provider. Every
    // relational provider keeps the option in its RelationalOptionsExtension, which EF Core stores under its runtime
    // type, so the copy replaces the provider's extension (docs/plan/RESEARCH.md R-126).
    private static void KeepMigrationHistoryIn(DbContextOptionsBuilder options, string schema)
    {
        var relational = options.Options.Extensions.OfType<RelationalOptionsExtension>().SingleOrDefault()
            ?? throw new InvalidOperationException(
                $"The database AddModuleDatabase gives {options.Options.ContextType.Name} is not a relational provider: "
                + "a module context keeps its migration history in its schema.");
        ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(
            relational.WithMigrationsHistoryTableSchema(schema));
    }
}
