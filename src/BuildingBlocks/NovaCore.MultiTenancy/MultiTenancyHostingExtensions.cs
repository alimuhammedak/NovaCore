using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Authorization;
using NovaCore.EntityFrameworkCore.MultiTenancy;

namespace NovaCore.MultiTenancy;

/// <summary>Adds multi-tenancy to the host: the tenants, their members, and which tenant a request works in.</summary>
public static class MultiTenancyHostingExtensions
{
    /// <summary>The route value a request names its tenant with by default: <c>/api/v1/tenants/{tenant}/...</c>.</summary>
    public const string TenantRouteValue = "tenant";

    /// <summary>
    /// Adds multi-tenancy, reading the tenant from the <see cref="TenantRouteValue"/> route value. A request works in the
    /// tenant it names only when the caller is a member of it and it is active, decided on the server for every request
    /// (<see cref="ITenantResolver"/>, cached with the caller's permissions): otherwise it has no tenant, its tenant
    /// entities are out of reach and <c>RequireTenant()</c> answers 403. In a request that works in a tenant, the caller
    /// also holds what their roles there grant: <c>MultiTenancy:Roles:&lt;role&gt;</c> lists a role's permissions, each
    /// one a module declares (the host doesn't start otherwise). A request that names the tenant holds them on whatever
    /// endpoint it calls, so a role grants the permissions of the tenant's endpoints only, never the platform's: a role with one of the
    /// identity provider's own permissions (<c>identity:*</c>) stops the host too. <see cref="TenantDirectory"/> manages the tenants, their members and their life cycle. The store is
    /// <see cref="MultiTenancyDbContext"/>: the host registers it with <c>AddModuleDbContext</c> and keeps its migrations.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static IHostApplicationBuilder AddCoreMultiTenancy(this IHostApplicationBuilder builder) =>
        builder.AddCoreMultiTenancy(context => context.GetRouteValue(TenantRouteValue)?.ToString());

    /// <summary>
    /// Adds multi-tenancy (<see cref="AddCoreMultiTenancy(IHostApplicationBuilder)"/>), reading the tenant with
    /// <paramref name="tenantOf"/>: a header (<c>context =&gt; context.Request.Headers["X-Tenant"]</c>), the host name
    /// or a query string. It returns the tenant's id; anything that isn't a Guid names no tenant. The tenant isn't taken
    /// on trust wherever it comes from: the caller must be a member of it.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="tenantOf">Reads the tenant's id from a request.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    public static IHostApplicationBuilder AddCoreMultiTenancy(this IHostApplicationBuilder builder, Func<HttpContext, string?> tenantOf)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(tenantOf);

        var services = builder.Services;
        services.AddOptions<MultiTenancyOptions>()
            .BindConfiguration(MultiTenancyOptions.SectionName)
            .Configure(options => options.TenantOf = tenantOf)
            .Validate<IEnumerable<IPermissionDefinitionProvider>>(
                (options, providers) => options.Roles.Values.SelectMany(permissions => permissions).All(
                    providers.SelectMany(provider => provider.GetPermissions())
                        .Select(permission => permission.Name)
                        .ToHashSet(StringComparer.Ordinal)
                        .Contains),
                "MultiTenancy:Roles must list permissions a module declares.")
            // A request that names its tenant holds the role's permissions on every endpoint it calls, the platform's
            // included when the tenant comes from a header: a role with identity:* would make a tenant's owner an
            // administrator of every account. The add-on doesn't reference the identity provider, so it knows the
            // provider's permissions by their prefix only (the seed's User role has the same rule).
            .Validate(
                options => !options.Roles.Values.SelectMany(permissions => permissions).Any(
                    permission => permission.StartsWith(MultiTenancyOptions.PlatformPermissionPrefix, StringComparison.Ordinal)),
                "MultiTenancy:Roles can't hold the identity provider's own permissions (identity:*): a tenant role grants "
                + "the tenant's endpoints only.")
            .ValidateOnStart();
        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddScoped<IPermissionResolver, MembershipPermissionResolver>();
        services.AddScoped(provider => new TenantDirectory(
            provider.GetRequiredService<MultiTenancyDbContext>(),
            provider.GetRequiredService<TenantData>(),
            provider.GetRequiredService<HybridCache>(),
            provider.GetRequiredService<IOptions<MultiTenancyOptions>>(),
            provider.GetRequiredService<TimeProvider>()));
        services.TryAddSingleton(TimeProvider.System);
        return builder;
    }
}
