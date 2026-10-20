using NovaCore.AspNetCore.Authorization;

namespace NovaCore.Identity.OpenIddict;

/// <summary>The permissions of the provider's admin API. Format: <c>module:resource:action</c>.</summary>
internal static class IdentityPermissions
{
    public const string Group = "Identity";

    public const string UsersRead = "identity:users:read";
    public const string UsersManage = "identity:users:manage";
    public const string RolesRead = "identity:roles:read";
    public const string RolesManage = "identity:roles:manage";
}

/// <summary>The two roles every seeded store has.</summary>
internal static class IdentityRoles
{
    /// <summary>
    /// Holds every permission the modules declare: the permission resolver grants them to its members, whether or not the
    /// seed has written them on the role yet.
    /// </summary>
    public const string Administrator = "Administrator";

    /// <summary>Every registered user: the business modules' read permissions, none of the admin ones.</summary>
    public const string User = "User";
}

internal sealed class IdentityPermissionProvider : IPermissionDefinitionProvider
{
    public IReadOnlyCollection<PermissionDefinition> GetPermissions() =>
    [
        new(IdentityPermissions.UsersRead, "View users", IdentityPermissions.Group),
        new(IdentityPermissions.UsersManage, "Manage users (roles, lockout)", IdentityPermissions.Group),
        new(IdentityPermissions.RolesRead, "View roles", IdentityPermissions.Group),
        new(IdentityPermissions.RolesManage, "Manage roles & their permissions", IdentityPermissions.Group)
    ];
}
