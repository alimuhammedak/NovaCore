using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Identity;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// DECISIONS Y-26: the test host validates scopes and the whole container on build. ValidateOnBuild already ran by the
/// time a test gets the host (a captive dependency would fail every integration test); this checks ValidateScopes.
/// </summary>
public class ServiceProviderValidationTests
{
    [Fact]
    public async Task Scoped_services_cannot_be_resolved_from_the_root_provider_even_outside_development()
    {
        // Production: the environment where the framework would leave validation off by default (R-029).
        await using var factory = new ProductionApiFactory();

        var error = Assert.Throws<InvalidOperationException>(() => factory.Services.GetRequiredService<ICurrentUser>());

        Assert.Contains("scoped", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
