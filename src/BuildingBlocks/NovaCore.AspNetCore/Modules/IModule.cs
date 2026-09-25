using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NovaCore.AspNetCore.Modules;

/// <summary>
/// A self-contained business module. It registers its own services and maps its own endpoints; the host only adds it
/// with <see cref="ModuleExtensions.AddModule{TModule}"/>.
/// </summary>
public interface IModule
{
    /// <summary>Stable module name (used for grouping, logging and OpenAPI tags).</summary>
    string Name { get; }

    /// <summary>Register the module's services (DbContext, handlers, validators, contracts).</summary>
    IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration);

    /// <summary>Map the module's HTTP endpoints.</summary>
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
