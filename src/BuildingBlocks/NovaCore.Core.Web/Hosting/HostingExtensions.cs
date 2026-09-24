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
using Serilog;
using Serilog.Events;

namespace NovaCore.Core.Web.Hosting;

/// <summary>
/// The NovaCore take on Aspire's "ServiceDefaults": one call wires structured logging (Serilog),
/// observability (OpenTelemetry traces + metrics), health checks and resilient HttpClients.
/// </summary>
public static class HostingExtensions
{
    public static IHostApplicationBuilder AddCoreDefaults(this IHostApplicationBuilder builder)
    {
        builder.AddCoreLogging();
        builder.AddCoreObservability();
        builder.AddCoreHealthChecks();

        // Every outbound HttpClient gets standard retry/circuit-breaker/timeout behaviour.
        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        return builder;
    }

    public static IHostApplicationBuilder AddCoreLogging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}");

            // Logs ship to Seq only when a server URL is configured — never a hard dependency.
            var seqUrl = builder.Configuration["Seq:ServerUrl"];
            if (!string.IsNullOrWhiteSpace(seqUrl))
                configuration.WriteTo.Seq(seqUrl, apiKey: builder.Configuration["Seq:ApiKey"]);
        });

        return builder;
    }

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
