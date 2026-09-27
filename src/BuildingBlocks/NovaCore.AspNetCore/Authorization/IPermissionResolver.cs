using System.Security.Claims;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>
/// Resolves, on the server, the permissions an authenticated caller holds: the access token says who the caller is, not
/// what they may do. The identity provider's integration implements it, e.g. from the user's roles in its store or by
/// mapping the provider's roles.
/// </summary>
public interface IPermissionResolver
{
    /// <summary>The permissions <paramref name="user"/> holds, e.g. <c>catalog:products:create</c>.</summary>
    /// <param name="user">The authenticated caller, as validated from the access token.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The permission names; empty when the caller is unknown or holds none.</returns>
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken);
}
