using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>
/// Finds the tenant a request is for and decides whether the caller may work in it, on the server, on every request
/// (Microsoft's "request validation"). The multi-tenancy add-on implements it from its store; register at most one. The
/// core asks it while it resolves the caller's permissions, so the tenant is known before authorization runs: a request
/// names a tenant (<see cref="RequestedTenant"/>), and the caller works in it only when <see cref="CanAccessAsync"/> says
/// so; the answer is cached with the caller's permissions, per subject, security stamp and tenant, for
/// <c>Cache:PermissionExpiration</c>.
/// </summary>
/// <remarks>
/// A tenant that isn't validated is no tenant: the request's <c>TenantContext</c> stays empty, the tenant's data stays
/// out of reach and <c>RequireTenant()</c> answers 403. Code that changes the answer drops the cached one at once:
/// <see cref="PermissionCacheExtensions.RemovePermissionsAsync"/> for a subject's memberships,
/// <see cref="PermissionCacheExtensions.RemoveTenantPermissionsAsync"/> for a tenant's status.
/// </remarks>
public interface ITenantResolver
{
    /// <summary>The tenant <paramref name="context"/> names (a route value, a header, the host), not yet validated; null when it names none.</summary>
    /// <param name="context">The request.</param>
    /// <returns>The tenant's id, or null.</returns>
    Guid? RequestedTenant(HttpContext context);

    /// <summary>Whether <paramref name="user"/> may work in <paramref name="tenantId"/> now: an active member of an active tenant.</summary>
    /// <param name="user">The authenticated caller, as validated from the access token.</param>
    /// <param name="tenantId">The tenant the request names.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>True when the caller may work in the tenant.</returns>
    Task<bool> CanAccessAsync(ClaimsPrincipal user, Guid tenantId, CancellationToken cancellationToken);
}
