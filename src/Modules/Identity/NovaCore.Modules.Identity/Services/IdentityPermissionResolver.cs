using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.Modules.Identity.Domain;

namespace NovaCore.Modules.Identity.Services;

/// <summary>
/// Resolves a caller's permissions from the Identity store by the token's <c>sub</c>: the <c>permission</c> claims of
/// every role the user holds, plus the user's own direct grants.
/// </summary>
public sealed class IdentityPermissionResolver(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : IPermissionResolver
{
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        // The store converts the id to a Guid and throws on anything else, so a foreign subject is rejected here.
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

        // Direct, user-specific permission grants augment role permissions.
        foreach (var claim in await userManager.GetClaimsAsync(account))
            if (claim.Type == NovaClaims.Permission)
                permissions.Add(claim.Value);

        return permissions;
    }
}
