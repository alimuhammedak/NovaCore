using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>Service registration and endpoint conventions for permission-based authorization.</summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Enables permission-based authorization: the handler that checks the <see cref="PermissionRequirement"/> a
    /// <see cref="RequirePermissionAttribute"/> carries, and the claims transformation that gives every authenticated
    /// caller the permissions <see cref="IPermissionResolver"/> resolves on the server, cached in HybridCache per
    /// subject and security stamp for <c>Cache:PermissionExpiration</c>. Permission claims in the access token are
    /// discarded. Secure by default: an endpoint without authorization metadata, and a request that matches no endpoint,
    /// requires an authenticated caller (the fallback policy); a public endpoint says so with <c>AllowAnonymous</c>.
    /// Needs <c>AddCoreAuthentication</c>, <c>AddCoreCaching</c> and a registered <see cref="IPermissionResolver"/> (the
    /// identity integration brings it; without one the host does not start); it replaces any other
    /// <see cref="IClaimsTransformation"/>.
    /// </summary>
    public static IServiceCollection AddCoreAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();
        services.AddHostedService<PermissionResolverStartupCheck>();

        // A forgotten RequirePermission must not publish an endpoint (OWASP API5, docs/plan/RESEARCH.md R-073).
        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
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
