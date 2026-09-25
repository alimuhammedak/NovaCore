using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace NovaCore.Core.Infrastructure.Persistence;

/// <summary>Records that a module owns a given DbContext, so the host can migrate them all generically.</summary>
public sealed record ModuleDbContextDescriptor(Type ContextType);

/// <summary>Service registration for module-owned DbContexts.</summary>
public static class PersistenceExtensions
{
    /// <summary>
    /// Registers a module's DbContext <em>and</em> advertises it for generic startup migration.
    /// Each module keeps its own context (and its own schema) — the modular-monolith boundary at the DB level.
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
        return services;
    }
}
