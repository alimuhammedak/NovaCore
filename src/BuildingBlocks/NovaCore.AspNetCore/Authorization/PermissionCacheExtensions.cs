using Microsoft.Extensions.Caching.Hybrid;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>Invalidation of the permissions the core caches per caller.</summary>
public static class PermissionCacheExtensions
{
    /// <summary>
    /// Drops the permissions cached for <paramref name="subject"/> (the token's <c>sub</c>), whatever security stamp and
    /// tenant they were cached under, so the subject's next request resolves them again through
    /// <see cref="IPermissionResolver"/> (and <see cref="ITenantResolver"/>). Call it after a change to what the resolvers
    /// return for the subject, e.g. a role granted or removed, a membership added or ended. It reaches
    /// this server's memory and the distributed cache; another server's memory keeps its entry until
    /// <c>Cache:PermissionExpiration</c> passes.
    /// </summary>
    /// <param name="cache">The host's <see cref="HybridCache"/>, which caches the permissions.</param>
    /// <param name="subject">The subject whose permissions changed.</param>
    /// <param name="cancellationToken">Cancels the removal.</param>
    /// <returns>A task that completes when the entries are invalidated.</returns>
    public static ValueTask RemovePermissionsAsync(this HybridCache cache, string subject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentException.ThrowIfNullOrEmpty(subject);

        return cache.RemoveByTagAsync(SubjectTag(subject), cancellationToken);
    }

    /// <summary>
    /// Drops every caller's cached permissions and tenant decision for <paramref name="tenantId"/>, so each caller's next
    /// request with that tenant asks <see cref="ITenantResolver"/> again. Call it after a change to whether the tenant
    /// may be worked in, e.g. the tenant suspended or reactivated. It reaches this server's memory and the distributed
    /// cache; another server's memory keeps its entries until <c>Cache:PermissionExpiration</c> passes.
    /// </summary>
    /// <param name="cache">The host's <see cref="HybridCache"/>, which caches the permissions.</param>
    /// <param name="tenantId">The tenant whose status changed.</param>
    /// <param name="cancellationToken">Cancels the removal.</param>
    /// <returns>A task that completes when the entries are invalidated.</returns>
    public static ValueTask RemoveTenantPermissionsAsync(this HybridCache cache, Guid tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);

        return cache.RemoveByTagAsync(TenantTag(tenantId), cancellationToken);
    }

    // The cache key also holds the security stamp, which the caller of RemovePermissionsAsync can't know: the entries are
    // found by this tag instead (docs/plan/RESEARCH.md R-100). Escaped like the key, so no subject collides with another.
    internal static string SubjectTag(string subject) => $"novacore:permissions:{Uri.EscapeDataString(subject)}";

    // An escaped subject has no ':', so no subject tag is a tenant tag.
    internal static string TenantTag(Guid tenantId) => $"novacore:permissions:tenant:{tenantId:D}";
}
