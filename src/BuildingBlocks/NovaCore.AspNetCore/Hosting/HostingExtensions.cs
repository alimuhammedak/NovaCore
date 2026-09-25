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

    /// <summary>
    /// In Development, maps <c>/health</c> (readiness — all checks, including each module database) and <c>/alive</c>
    /// (liveness — tagged "live"). Other environments get neither: a host that needs probes there maps its own.
    /// </summary>
    public static IEndpointRouteBuilder MapDefaultEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // The endpoints reveal the app's state and every /health request reaches every database, so they stay out of
        // non-Development environments (docs/plan/DECISIONS.md D-022). A production host can map its own, e.g. behind
        // a check on ConnectionInfo.LocalPort; RequireHost alone trusts the spoofable Host header (R-049).
        if (!endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
            return endpoints;

        endpoints.MapHealthChecks("/health");

        endpoints.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live")
        });

        return endpoints;
    }
}
