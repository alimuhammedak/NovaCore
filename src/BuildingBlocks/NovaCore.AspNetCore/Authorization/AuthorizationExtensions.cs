using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>Service registration and endpoint conventions for permission-based authorization.</summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Enables permission-based authorization: the handler that checks the <see cref="PermissionRequirement"/> a
    /// <see cref="RequirePermissionAttribute"/> carries. Call after <c>AddCoreAuthentication</c>.
    /// </summary>
    public static IServiceCollection AddCoreAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();
        return services;
    }

    /// <summary>Registers a module's permission definitions for seeding/discovery.</summary>
    public static IServiceCollection AddPermissionDefinitions<TProvider>(this IServiceCollection services)
        where TProvider : class, IPermissionDefinitionProvider
    {
        services.AddSingleton<IPermissionDefinitionProvider, TProvider>();
        return services;
    }

    /// <summary>
    /// Guards an endpoint (or group) with a fine-grained permission: <c>.RequirePermission("catalog:products:create")</c>.
    /// Anonymous callers get 401; authenticated callers without the permission (or the super-admin role) get 403.
    /// </summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(new RequirePermissionAttribute(permission));
}
