using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NovaCore.OpenTelemetry;

/// <summary>Adds OpenTelemetry observability to the host.</summary>
public static class OpenTelemetryHostingExtensions
{
    /// <summary>
    /// Adds OpenTelemetry metrics and tracing for ASP.NET Core, HttpClient and the runtime, exported over OTLP when
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is configured.
    /// </summary>
    public static IHostApplicationBuilder AddCoreObservability(this IHostApplicationBuilder builder)
    {
        var otel = builder.Services.AddOpenTelemetry();

        otel.ConfigureResource(resource => resource.AddService(
            serviceName: builder.Environment.ApplicationName,
            serviceVersion: typeof(OpenTelemetryHostingExtensions).Assembly.GetName().Version?.ToString()));

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
}
