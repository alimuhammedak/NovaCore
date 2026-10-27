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
    /// Adds OpenTelemetry logs, metrics and tracing: the host's <c>ILogger</c> records (with their rendered message and
    /// scopes), metrics and traces for ASP.NET Core, HttpClient and the runtime, and the outbox handlers' spans. All three
    /// are exported over OTLP when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is configured, so the host needs no second logging
    /// pipeline.
    /// </summary>
    public static IHostApplicationBuilder AddCoreObservability(this IHostApplicationBuilder builder)
    {
        var otel = builder.Services.AddOpenTelemetry();

        otel.ConfigureResource(resource => resource.AddService(
            serviceName: builder.Environment.ApplicationName,
            serviceVersion: typeof(OpenTelemetryHostingExtensions).Assembly.GetName().Version?.ToString()));

        // Logs travel with the traces and metrics, as in Aspire's ServiceDefaults. A record keeps its rendered message
        // and its scopes (the request's, among others), which the OTLP record would otherwise drop.
        otel.WithLogging(configureBuilder: null, configureOptions: logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        otel.WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation());

        // NovaCore.EntityFrameworkCore's outbox dispatcher runs the event handlers in spans of its own, which continue the
        // trace of the request that raised the event. The source's name is that assembly's; this add-on doesn't reference it.
        otel.WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("NovaCore.EntityFrameworkCore"));

        // Export all three over OTLP only when an endpoint is configured (e.g. an OTel collector / Aspire dashboard).
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            otel.UseOtlpExporter();

        return builder;
    }
}
