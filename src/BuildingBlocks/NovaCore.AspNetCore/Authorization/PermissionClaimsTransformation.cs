using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Caching;
using NovaCore.AspNetCore.Identity;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>
/// Replaces whatever permissions an access token claims with those <see cref="IPermissionResolver"/> resolves on the
/// server, cached in <see cref="HybridCache"/> per subject and security stamp for <c>Cache:PermissionExpiration</c>. A
/// role change therefore reaches a signed-in caller within that lifetime, and a token with a new stamp skips the cache.
/// </summary>
internal sealed class PermissionClaimsTransformation(
    IPermissionResolver resolver,
    HybridCache cache,
    IOptions<CacheOptions> cacheOptions) : IClaimsTransformation
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

        var permissions = await cache.GetOrCreateAsync(
            CacheKey(subject, transformed.FindFirstValue(NovaClaims.SecurityStamp)),
            (resolver, transformed),
            static async (state, cancellationToken) =>
                (await state.resolver.GetPermissionsAsync(state.transformed, cancellationToken)).ToArray(),
            _entryOptions,
            // No caller token: the lookup runs on the first caller's scoped resolver and concurrent callers share it
            // (stampede protection), so that caller waits for it to finish instead of leaving and disposing its scope.
            cancellationToken: CancellationToken.None);

        transformed.Identities.First(identity => identity.IsAuthenticated)
            .AddClaims(permissions.Select(permission => new Claim(NovaClaims.Permission, permission)));
        return transformed;
    }

    // Escaped, so no subject or stamp can contain the separator and collide with another pair.
    private static string CacheKey(string subject, string? securityStamp) =>
        $"novacore:permissions:{Uri.EscapeDataString(subject)}:{Uri.EscapeDataString(securityStamp ?? string.Empty)}";
}
