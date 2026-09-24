using System.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Core.Abstractions.Caching;
using NovaCore.Core.Abstractions.Messaging;
using NovaCore.Core.Infrastructure.Caching;
using NovaCore.Core.Infrastructure.Exceptions;
using NovaCore.Core.Infrastructure.Messaging;
using NovaCore.Core.Infrastructure.Persistence;

namespace NovaCore.Core.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Wires the cross-cutting infrastructure: ProblemDetails, exception handling, caching, auditing, eventing.</summary>
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

        // Audit stamping runs inside the unit of work; scoped so it sees the request's ICurrentUser.
        services.AddScoped<AuditableEntityInterceptor>();

        // In-process domain-event dispatch — no commercial mediator required.
        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

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
