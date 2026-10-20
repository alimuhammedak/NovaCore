using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NovaCore.OpenTelemetry;
using OpenTelemetry.Logs;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// NovaCore.OpenTelemetry carries the host's logs with its metrics and traces, so a host needs no second logging pipeline
/// (PLAN F5.1). A log record keeps its rendered message and its scopes, the request's among them.
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
}
