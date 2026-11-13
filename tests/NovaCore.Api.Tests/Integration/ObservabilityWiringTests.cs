using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>The Api host gives OpenTelemetry its own assembly, so its telemetry names the Api's version.</summary>
public class ObservabilityWiringTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    [Fact]
    public void The_api_reports_its_own_version_as_the_service_version()
    {
        var api = typeof(Program).Assembly;

        var resource = factory.Services.GetRequiredService<TracerProvider>().GetResource().Attributes
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value, StringComparer.Ordinal);

        Assert.Equal(api.GetName().Name, resource["service.name"]);
        Assert.Equal(api.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion, resource["service.version"]);
    }
}
