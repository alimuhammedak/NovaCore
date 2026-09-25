using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace NovaCore.Core.Web.Authorization;

/// <summary>Service registration and endpoint conventions for permission-based authorization.</summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Enables permission-based authorization: the dynamic policy provider + the permission handler.
    /// Call after <c>AddCoreAuthentication</c>.
    /// </summary>
    public static IServiceCollection AddCoreAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
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

    /// <summary>Guards an endpoint (or group) with a fine-grained permission: <c>.RequirePermission("catalog:products:create")</c>.</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(PermissionPolicyProvider.PolicyPrefix + permission);
}
