using System.Reflection;
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
    /// <remarks>
    /// The resource's <c>service.name</c> is <see cref="IHostEnvironment.ApplicationName"/>. This overload reports no
    /// <c>service.version</c> (one in <c>OTEL_RESOURCE_ATTRIBUTES</c> still applies): the overload taking the application's
    /// assembly reports that assembly's version.
    /// </remarks>
    public static IHostApplicationBuilder AddCoreObservability(this IHostApplicationBuilder builder) =>
        builder.AddObservability(serviceVersion: null);

    /// <summary>
    /// Adds OpenTelemetry like <see cref="AddCoreObservability(IHostApplicationBuilder)"/>, with the application's version
    /// as the resource's <c>service.version</c>: the informational version of <paramref name="applicationAssembly"/>
    /// (the product version, with the source revision), else its assembly version.
    /// </summary>
    /// <param name="builder">The host's builder.</param>
    /// <param name="applicationAssembly">The application's own assembly, e.g. <c>typeof(Program).Assembly</c> in the host.</param>
    public static IHostApplicationBuilder AddCoreObservability(this IHostApplicationBuilder builder, Assembly applicationAssembly)
    {
        ArgumentNullException.ThrowIfNull(applicationAssembly);

        return builder.AddObservability(
            applicationAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? applicationAssembly.GetName().Version?.ToString());
    }

    private static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder, string? serviceVersion)
    {
        var otel = builder.Services.AddOpenTelemetry();

        // The add-on's own version is no application's: without the application's assembly the resource carries none.
        otel.ConfigureResource(resource => resource.AddService(
            serviceName: builder.Environment.ApplicationName,
            serviceVersion: serviceVersion));

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
