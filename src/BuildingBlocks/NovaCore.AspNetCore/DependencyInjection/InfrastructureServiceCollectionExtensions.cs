using System.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Caching;
using NovaCore.AspNetCore.Exceptions;

namespace NovaCore.AspNetCore.DependencyInjection;

/// <summary>Service registration for the cross-cutting infrastructure.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Wires the cross-cutting infrastructure: ProblemDetails, exception handling and caching.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services)
    {
        // RFC 9457 ProblemDetails, enriched with a correlation id every response carries. "instance" is a URI reference
        // (R-051): the request path, without the query string, which may carry secrets.
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var request = context.HttpContext.Request;
            var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            context.ProblemDetails.Extensions["traceId"] = traceId;
            context.ProblemDetails.Instance ??= request.PathBase.Add(request.Path).ToUriComponent();
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddCoreCaching();
        return services;
    }

    /// <summary>
    /// Registers <see cref="HybridCache"/> (L1 in-memory), with the default entry lifetime from
    /// <c>Cache:DefaultExpiration</c> (validated on start). Consumers inject <see cref="HybridCache"/> directly. To add
    /// an L2 distributed tier, register an <c>IDistributedCache</c> (e.g. Redis) — HybridCache adopts it automatically.
    /// </summary>
    public static IServiceCollection AddCoreCaching(this IServiceCollection services)
    {
        services.AddOptions<CacheOptions>()
            .BindConfiguration(CacheOptions.SectionName)
            .Validate(cache => cache.DefaultExpiration > TimeSpan.Zero,
                "Cache:DefaultExpiration must be a positive time span, e.g. 00:05:00.")
            .ValidateOnStart();

        services.AddHybridCache();
        services.AddOptions<HybridCacheOptions>().Configure<IOptions<CacheOptions>>((options, cache) =>
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = cache.Value.DefaultExpiration,
                LocalCacheExpiration = cache.Value.DefaultExpiration
            });

        return services;
    }
}
