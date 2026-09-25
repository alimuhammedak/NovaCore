using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NovaCore.AspNetCore.Modules;

/// <summary>Registers and maps the application's <see cref="IModule"/>s.</summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TModule"/>'s services and adds the module instance to DI, so
    /// <see cref="MapModules"/> maps endpoints from the exact same instance. Modules are mapped in the order they are added.
    /// </summary>
    /// <typeparam name="TModule">The module to add.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static IHostApplicationBuilder AddModule<TModule>(this IHostApplicationBuilder builder)
        where TModule : class, IModule, new()
    {
        var module = new TModule();
        module.AddModule(builder.Services, builder.Configuration);
        builder.Services.AddSingleton<IModule>(module);

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
