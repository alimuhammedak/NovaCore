using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using NovaCore.Abstractions.MultiTenancy;
using NovaCore.AspNetCore.Caching;
using NovaCore.AspNetCore.Identity;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>
/// Replaces whatever permissions an access token claims with those <see cref="IPermissionResolver"/> resolves on the
/// server, cached in <see cref="HybridCache"/> per subject and security stamp for <c>Cache:PermissionExpiration</c>. A
/// role change therefore reaches a signed-in caller within that lifetime, and a token with a new stamp skips the cache.
/// With an <see cref="ITenantResolver"/>, the request's tenant is decided here too, before authorization: the tenant the
/// request names becomes the scope's (<see cref="TenantContext"/>) only when the resolver lets the caller work in it, and
/// that decision is cached with the permissions, per tenant. The entries carry the subject's tag and the tenant's, so
/// whoever changes the permissions or the tenant drops them at once (<see cref="PermissionCacheExtensions"/>).
/// </summary>
internal sealed class PermissionClaimsTransformation(
    IPermissionResolver resolver,
    HybridCache cache,
    IOptions<CacheOptions> cacheOptions,
    TenantContext tenantContext,
    IHttpContextAccessor httpContextAccessor,
    ITenantResolver? tenantResolver = null) : IClaimsTransformation
{
    private readonly HybridCacheEntryOptions _entryOptions = new()
    {
        Expiration = cacheOptions.Value.PermissionExpiration,
        LocalCacheExpiration = cacheOptions.Value.PermissionExpiration
    };

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        // This runs on every AuthenticateAsync call (docs/plan/RESEARCH.md R-057). Work on a copy and derive the result
        // from the token alone, so a repeated call gives the same principal and the handler's own result is untouched.
        var transformed = principal.Clone();
        foreach (var identity in transformed.Identities)
            foreach (var claim in identity.FindAll(NovaClaims.Permission).ToList())
                identity.TryRemoveClaim(claim);

        var subject = transformed.FindFirstValue(NovaClaims.Subject);
        if (string.IsNullOrEmpty(subject))
            return transformed;

        var requested = tenantResolver is not null && httpContextAccessor.HttpContext is { } request
            ? tenantResolver.RequestedTenant(request)
            : null;

        var grant = await cache.GetOrCreateAsync(
            CacheKey(subject, transformed.FindFirstValue(NovaClaims.SecurityStamp), requested),
            (resolver, tenantResolver, tenantContext, transformed, requested),
            static async (state, cancellationToken) =>
            {
                Guid? tenant = null;
                if (state.requested is { } tenantId
                    && await state.tenantResolver!.CanAccessAsync(state.transformed, tenantId, cancellationToken))
                {
                    // The tenant-aware resolvers read the scope's tenant.
                    state.tenantContext.Use(tenantId);
                    tenant = tenantId;
                }

                var permissions = await state.resolver.GetPermissionsAsync(state.transformed, cancellationToken);
                return new PermissionGrant(tenant, [.. permissions]);
            },
            _entryOptions,
            requested is { } named
                ? [PermissionCacheExtensions.SubjectTag(subject), PermissionCacheExtensions.TenantTag(named)]
                : [PermissionCacheExtensions.SubjectTag(subject)],
            // No caller token: the lookup runs on the first caller's scoped resolver and concurrent callers share it
            // (stampede protection), so that caller waits for it to finish instead of leaving and disposing its scope.
            cancellationToken: CancellationToken.None);

        if (grant.Tenant is { } validated)
            tenantContext.Use(validated);

        transformed.Identities.First(identity => identity.IsAuthenticated)
            .AddClaims(grant.Permissions.Select(permission => new Claim(NovaClaims.Permission, permission)));
        return transformed;
    }

    // Escaped, so no subject or stamp can contain the separator and collide with another pair.
    private static string CacheKey(string subject, string? securityStamp, Guid? tenant) =>
        $"novacore:permissions:{Uri.EscapeDataString(subject)}:{Uri.EscapeDataString(securityStamp ?? string.Empty)}:{tenant:D}";
}

/// <summary>
/// What the cache keeps for a caller: the tenant they may work in for the tenant the request named (null when it named
/// none or the caller may not), and the permissions resolved with it.
/// </summary>
internal sealed record PermissionGrant(Guid? Tenant, string[] Permissions);
