using NovaCore.Core.Web.Authorization;

namespace NovaCore.Modules.Identity.Authorization;

/// <summary>Permissions owned by the Identity module. Format: <c>module:resource:action</c>.</summary>
public static class IdentityPermissions
{
    public const string Group = "Identity";

    public const string UsersRead = "identity:users:read";
    public const string UsersManage = "identity:users:manage";
    public const string RolesRead = "identity:roles:read";
    public const string RolesManage = "identity:roles:manage";
}

public sealed class IdentityPermissionProvider : IPermissionDefinitionProvider
{
    public IReadOnlyCollection<PermissionDefinition> GetPermissions() =>
    [
        new(IdentityPermissions.UsersRead, "View users", IdentityPermissions.Group),
        new(IdentityPermissions.UsersManage, "Manage users (roles, lockout)", IdentityPermissions.Group),
        new(IdentityPermissions.RolesRead, "View roles", IdentityPermissions.Group),
        new(IdentityPermissions.RolesManage, "Manage roles & their permissions", IdentityPermissions.Group)
    ];
}
