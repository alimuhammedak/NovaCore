using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Resolves a caller's permissions from the identity store by the token's <c>sub</c>: the <c>permission</c> claims of
/// every role the user holds, plus the user's own direct grants.
/// </summary>
internal sealed class IdentityPermissionResolver(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : IPermissionResolver
{
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        // User ids are Guids and the store throws on anything else, so any other subject holds nothing here.
        if (!Guid.TryParse(user.FindFirstValue(NovaClaims.Subject), out var userId))
            return [];

        var account = await userManager.FindByIdAsync(userId.ToString());
        if (account is null)
            return [];

        var permissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var roleName in await userManager.GetRolesAsync(account))
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
                continue;

            foreach (var claim in await roleManager.GetClaimsAsync(role))
                if (claim.Type == NovaClaims.Permission)
                    permissions.Add(claim.Value);
        }

        foreach (var claim in await userManager.GetClaimsAsync(account))
            if (claim.Type == NovaClaims.Permission)
                permissions.Add(claim.Value);

        return permissions;
    }
}
