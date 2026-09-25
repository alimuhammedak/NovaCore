using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NovaCore.Core.Web.Modules;

/// <summary>Registers and maps the application's <see cref="IModule"/>s.</summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Discovers every enabled <see cref="IModule"/> and registers its services. Each module instance
    /// is also added to DI so <see cref="MapModules"/> can map endpoints from the exact same instances.
    /// </summary>
    public static IHostApplicationBuilder AddModules(this IHostApplicationBuilder builder, params string[] assemblyPrefixes)
    {
        foreach (var module in ModuleLoader.Discover(assemblyPrefixes))
        {
            if (!module.IsEnabled(builder.Configuration))
                continue;

            module.AddModule(builder.Services, builder.Configuration);
            builder.Services.AddSingleton(module);
        }

        return builder;
    }

    /// <summary>Maps the endpoints of every registered module.</summary>
    public static IEndpointRouteBuilder MapModules(this IEndpointRouteBuilder endpoints)
    {
        foreach (var module in endpoints.ServiceProvider.GetServices<IModule>())
            module.MapEndpoints(endpoints);

        return endpoints;
    }
}
