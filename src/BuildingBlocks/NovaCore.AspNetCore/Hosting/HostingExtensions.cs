using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace NovaCore.AspNetCore.Hosting;

/// <summary>
/// The NovaCore take on Aspire's "ServiceDefaults": one call wires health checks and resilient HttpClients. Logging
/// and observability are add-ons: <c>AddCoreLogging</c> (NovaCore.Serilog) and <c>AddCoreObservability</c>
/// (NovaCore.OpenTelemetry).
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Adds the health checks (see <see cref="AddCoreHealthChecks"/>) and the standard resilience handler on every
    /// HttpClient.
    /// </summary>
    public static IHostApplicationBuilder AddCoreDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddCoreHealthChecks();

        // Every outbound HttpClient gets standard retry/circuit-breaker/timeout behaviour.
        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        return builder;
    }

    /// <summary>Adds the health-check services with a liveness check tagged <c>live</c>.</summary>
    public static IHostApplicationBuilder AddCoreHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Liveness: the process is up. Tagged "live" so /alive can filter to just this.
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return builder;
    }

    /// <summary>Maps <c>/health</c> (readiness — all checks) and <c>/alive</c> (liveness — tagged "live").</summary>
    public static IEndpointRouteBuilder MapDefaultEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");

        endpoints.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live")
        });

        return endpoints;
    }
}
