using System.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Caching;
using NovaCore.Core.Infrastructure.Caching;
using NovaCore.Core.Infrastructure.Exceptions;

namespace NovaCore.Core.Infrastructure.DependencyInjection;

/// <summary>Service registration for the cross-cutting infrastructure.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Wires the cross-cutting infrastructure: ProblemDetails, exception handling and caching.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // RFC 9457 ProblemDetails, enriched with a correlation id every response carries.
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            context.ProblemDetails.Extensions["traceId"] = traceId;
            context.ProblemDetails.Instance ??= $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddCoreCaching(configuration);
        return services;
    }

    /// <summary>
    /// Registers HybridCache (L1 in-memory) behind <see cref="ICacheService"/>. To add an L2
    /// distributed tier, register an <c>IDistributedCache</c> (e.g. Redis) — HybridCache adopts it automatically.
    /// </summary>
    public static IServiceCollection AddCoreCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var defaultExpiration = configuration.GetValue<TimeSpan?>("Cache:DefaultExpiration") ?? TimeSpan.FromMinutes(5);

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = defaultExpiration,
                LocalCacheExpiration = defaultExpiration
            };
        });

        services.AddSingleton<ICacheService, HybridCacheService>();
        return services;
    }
}
