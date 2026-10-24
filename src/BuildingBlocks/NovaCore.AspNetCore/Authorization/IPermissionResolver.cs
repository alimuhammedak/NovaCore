using System.Security.Claims;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>
/// Resolves, on the server, the permissions an authenticated caller holds: the access token says who the caller is, not
/// what they may do. The identity provider's integration implements it, e.g. from the user's roles in its store. Every
/// registered resolver is asked and the caller holds what any of them grants: the identity provider's platform
/// permissions and, in a request that works in a tenant (<c>TenantContext</c>), what the caller's membership there
/// grants.
/// </summary>
/// <remarks>
/// The result is cached per subject (<c>sub</c>) and security stamp for <c>Cache:PermissionExpiration</c>, and served to
/// every token of that subject in the meantime, per tenant. So it must depend on who the caller is (and the scope's
/// tenant), not on which token they present: permissions narrowed by a token's scopes, client or audience would leak
/// from one token to another.
/// </remarks>
public interface IPermissionResolver
{
    /// <summary>The permissions <paramref name="user"/> holds, e.g. <c>catalog:products:create</c>.</summary>
    /// <param name="user">The authenticated caller, as validated from the access token.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The permission names; empty when the caller is unknown or holds none.</returns>
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken);
}
