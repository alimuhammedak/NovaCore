using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaCore.AspNetCore.Modules;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

public class ModuleRegistrationGuardTests
{
    [Fact]
    public void Adding_the_same_module_twice_fails_at_registration()
    {
        // A second AddModule would register the module's services twice and map every endpoint twice, which only
        // surfaces as an ambiguous route on the first request.
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.AddModule<SampleModule>();

        var error = Assert.Throws<InvalidOperationException>(() => builder.AddModule<SampleModule>());

        Assert.Contains(nameof(SampleModule), error.Message, StringComparison.Ordinal);
    }

    private sealed class SampleModule : IModule
    {
        public string Name => "Sample";

        public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration) => services;

        public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints;
    }
}
