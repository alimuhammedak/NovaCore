using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NovaCore.AspNetCore.Hosting;

/// <summary>
/// The NovaCore take on Aspire's "ServiceDefaults": one call wires observability (OpenTelemetry traces + metrics),
/// health checks and resilient HttpClients. Structured logging is the NovaCore.Serilog add-on's <c>AddCoreLogging</c>.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Adds observability and health checks (see the <c>AddCore*</c> methods below) and the standard resilience handler
    /// on every HttpClient.
    /// </summary>
    public static IHostApplicationBuilder AddCoreDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddCoreObservability();
        builder.AddCoreHealthChecks();

        // Every outbound HttpClient gets standard retry/circuit-breaker/timeout behaviour.
        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        return builder;
    }

    /// <summary>
    /// Adds OpenTelemetry metrics and tracing for ASP.NET Core, HttpClient and the runtime, exported over OTLP when
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is configured.
    /// </summary>
    public static IHostApplicationBuilder AddCoreObservability(this IHostApplicationBuilder builder)
    {
        var otel = builder.Services.AddOpenTelemetry();

        otel.ConfigureResource(resource => resource.AddService(
            serviceName: builder.Environment.ApplicationName,
            serviceVersion: typeof(HostingExtensions).Assembly.GetName().Version?.ToString()));

        otel.WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation());

        otel.WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation());

        // Export over OTLP only when an endpoint is configured (e.g. an OTel collector / Aspire dashboard).
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            otel.UseOtlpExporter();

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
