using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Resolves a caller's permissions by the token's <c>sub</c>. A user (a Guid subject) holds the <c>permission</c> claims
/// of every role they hold in the store, plus their own direct grants; a member of the Administrator role holds every
/// permission the modules declare. A service client (its client id as the subject) holds the permissions configured for
/// it in <c>Identity:Clients</c>.
/// </summary>
internal sealed class IdentityPermissionResolver(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IEnumerable<IPermissionDefinitionProvider> permissionProviders,
    IOptions<IdentityServerOptions> options) : IPermissionResolver
{
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var subject = user.FindFirstValue(NovaClaims.Subject);

        // Client ids are never Guids (validated on start), so a subject that isn't one is a client or unknown.
        if (!Guid.TryParse(subject, out var userId))
        {
            var client = options.Value.Clients.Find(client => !client.SignsInUsers && client.ClientId == subject);
            return client?.Permissions ?? [];
        }

        var account = await userManager.FindByIdAsync(userId.ToString());
        if (account is null)
            return [];

        var permissions = new HashSet<string>(StringComparer.Ordinal);
        var roleNames = await userManager.GetRolesAsync(account);

        // From the store, not the token's role claim, so a removed administrator loses it within the cache lifetime.
        // From the declarations, not the role's claims, so a module added since the last seed is covered too.
        if (roleNames.Contains(IdentityRoles.Administrator))
            permissions.UnionWith(permissionProviders.SelectMany(provider => provider.GetPermissions()).Select(permission => permission.Name));

        foreach (var roleName in roleNames)
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
