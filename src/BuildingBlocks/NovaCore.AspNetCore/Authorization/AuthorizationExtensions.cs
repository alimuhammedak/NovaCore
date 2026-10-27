using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NovaCore.Abstractions.MultiTenancy;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>Service registration and endpoint conventions for permission-based authorization.</summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Enables permission-based authorization: the handler that checks the <see cref="PermissionRequirement"/> a
    /// <see cref="RequirePermissionAttribute"/> carries, and the claims transformation that gives every authenticated
    /// caller the permissions the registered <see cref="IPermissionResolver"/>s resolve on the server (what any of them
    /// grants), cached in HybridCache per subject and security stamp for <c>Cache:PermissionExpiration</c>. Permission
    /// claims in the access token are discarded. Secure by default: an endpoint without authorization metadata, and a
    /// request that matches no endpoint, requires an authenticated caller (the fallback policy); a public endpoint says
    /// so with <c>AllowAnonymous</c>.
    /// Needs <c>AddCoreAuthentication</c>, <c>AddCoreCaching</c> and a registered <see cref="IPermissionResolver"/> (the
    /// identity integration brings it; without one the host does not start). ASP.NET Core runs one
    /// <see cref="IClaimsTransformation"/>, so the host does not start with another one registered either. With an
    /// <see cref="ITenantResolver"/> (the multi-tenancy add-on), the same transformation decides the request's tenant
    /// (<see cref="TenantContext"/>, registered here) and caches the permissions per tenant too.
    /// </summary>
    public static IServiceCollection AddCoreAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, TenantAuthorizationHandler>();
        services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();
        services.AddHttpContextAccessor();
        services.TryAddScoped<TenantContext>();
        services.AddHostedService<PermissionResolverStartupCheck>();
        services.AddHostedService<ClaimsTransformationStartupCheck>();

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
    /// Anonymous callers get 401; authenticated callers without the permission get 403.
    /// </summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(new RequirePermissionAttribute(permission));

    /// <summary>
    /// Guards an endpoint (or group) that works in a tenant: the request must have a tenant the caller may work in, as
    /// <see cref="ITenantResolver"/> decides (the scope's <see cref="TenantContext"/>), e.g. a group under
    /// <c>/api/v1/tenants/{tenant}</c>. Anonymous callers get 401; a caller without that tenant gets 403, whether the
    /// tenant the request names exists or not.
    /// </summary>
    public static TBuilder RequireTenant<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(policy => policy.RequireAuthenticatedUser().AddRequirements(new TenantRequirement()));
}

/// <summary>Requires the request to have a validated tenant (<see cref="AuthorizationExtensions.RequireTenant"/>).</summary>
internal sealed class TenantRequirement : IAuthorizationRequirement;

/// <summary>
/// Grants <see cref="TenantRequirement"/> when the scope has a tenant. The core sets it while it resolves the caller's
/// permissions, only for a tenant the caller may work in (<see cref="PermissionClaimsTransformation"/>).
/// </summary>
internal sealed class TenantAuthorizationHandler(TenantContext tenantContext) : AuthorizationHandler<TenantRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantRequirement requirement)
    {
        if (tenantContext.TenantId is not null)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
