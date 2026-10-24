using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        // The source the outbox dispatcher starts a handler's span from (NovaCore.EntityFrameworkCore, internal). Listeners
        // are process-wide: the dispatchers of the tests running alongside end spans of that source too, so this test
        // counts its own trace only.
        using var outbox = new ActivitySource("NovaCore.EntityFrameworkCore");
        var trace = ActivityTraceId.CreateRandom();
        var parent = new ActivityContext(trace, ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        outbox.StartActivity("process", ActivityKind.Consumer, parent)?.Dispose();

        Assert.Equal(["process"], ended.In(trace));
    }

    private sealed class EndedSpans : BaseProcessor<Activity>
    {
        private readonly ConcurrentQueue<(ActivityTraceId Trace, string Name)> _spans = new();

        public string[] In(ActivityTraceId trace) =>
            [.. _spans.Where(span => span.Trace == trace).Select(span => span.Name)];

        public override void OnEnd(Activity data) => _spans.Enqueue((data.TraceId, data.DisplayName));
    }
}
