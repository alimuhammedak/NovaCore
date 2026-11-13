using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NovaCore.EntityFrameworkCore.Messaging;
using NovaCore.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// NovaCore.OpenTelemetry carries the host's logs with its metrics and traces, so a host needs no second logging pipeline
/// (PLAN F5.1). A log record keeps its rendered message and its scopes, the request's among them. The traces take the
/// outbox handlers' spans too (PLAN F7b.5).
/// </summary>
public class ObservabilityTests
{
    [Fact]
    public void Logs_go_through_OpenTelemetry_with_their_message_and_scopes()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddCoreObservability();
        using var host = builder.Build();

        Assert.Contains(host.Services.GetServices<ILoggerProvider>(), provider => provider is OpenTelemetryLoggerProvider);
        var options = host.Services.GetRequiredService<IOptions<OpenTelemetryLoggerOptions>>().Value;
        Assert.True(options.IncludeFormattedMessage);
        Assert.True(options.IncludeScopes);
    }

    [Fact]
    public void Traces_take_the_outbox_handlers_spans()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddCoreObservability();
        using var ended = new EndedSpans();
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddProcessor(ended));
        using var host = builder.Build();
        _ = host.Services.GetRequiredService<TracerProvider>();

        // The source the outbox dispatcher starts a handler's span from, read from the dispatcher (internal), so a rename on
        // either side fails here: the add-on names it with a string, as it doesn't reference that package. Listeners are
        // process-wide: the dispatchers of the tests running alongside end spans of that source too, so this test counts
        // its own trace only.
        var sourceName = (string)typeof(OutboxInterceptor).Assembly
            .GetType("NovaCore.EntityFrameworkCore.Messaging.OutboxDispatcher", throwOnError: true)!
            .GetField("ActivitySourceName")!
            .GetRawConstantValue()!;
        using var outbox = new ActivitySource(sourceName);
        var trace = ActivityTraceId.CreateRandom();
        var parent = new ActivityContext(trace, ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        outbox.StartActivity("process", ActivityKind.Consumer, parent)?.Dispose();

        Assert.Equal(["process"], ended.In(trace));
    }

    [Fact]
    public void The_service_version_is_the_host_applications_not_the_add_ons()
    {
        // Every NovaCore assembly carries the repository's one VersionPrefix, so the host here is an assembly whose version
        // differs from the add-on's: the resource has to name the application the environment points at.
        var host = typeof(HostApplicationBuilder).Assembly;
        var expected = host.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.DoesNotContain(expected, AddOnVersions(), StringComparer.Ordinal);

        var resource = ResourceOf(host.GetName().Name!);

        Assert.Equal(host.GetName().Name, resource["service.name"]);
        Assert.Equal(expected, resource["service.version"]);
    }

    [Fact]
    public void An_application_name_that_is_no_assembly_reports_no_service_version()
    {
        var resource = ResourceOf("not-an-assembly");

        Assert.Equal("not-an-assembly", resource["service.name"]);
        Assert.False(resource.ContainsKey("service.version"));
    }

    private static Dictionary<string, object> ResourceOf(string applicationName)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ApplicationName = applicationName });
        builder.AddCoreObservability();
        using var host = builder.Build();

        return host.Services.GetRequiredService<TracerProvider>().GetResource().Attributes
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value, StringComparer.Ordinal);
    }

    private static string?[] AddOnVersions()
    {
        var addOn = typeof(OpenTelemetryHostingExtensions).Assembly;
        return
        [
            addOn.GetName().Version?.ToString(),
            addOn.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        ];
    }

    private sealed class EndedSpans : BaseProcessor<Activity>
    {
        private readonly ConcurrentQueue<(ActivityTraceId Trace, string Name)> _spans = new();

        public string[] In(ActivityTraceId trace) =>
            [.. _spans.Where(span => span.Trace == trace).Select(span => span.Name)];

        public override void OnEnd(Activity data) => _spans.Enqueue((data.TraceId, data.DisplayName));
    }
}
