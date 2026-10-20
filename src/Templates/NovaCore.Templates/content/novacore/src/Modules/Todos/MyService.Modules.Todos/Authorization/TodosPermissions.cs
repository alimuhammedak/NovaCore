using NovaCore.AspNetCore.Authorization;

namespace MyService.Modules.Todos.Authorization;

/// <summary>Permissions owned by the Todos module. Format: <c>module:resource:action</c>.</summary>
public static class TodosPermissions
{
    public const string Group = "Todos";

    public const string ItemsRead = "todos:items:read";
    public const string ItemsWrite = "todos:items:write";
}

/// <summary>Declares the module's permissions, so they can be granted (and seeded to roles by the identity provider).</summary>
public sealed class TodosPermissionProvider : IPermissionDefinitionProvider
{
    public IReadOnlyCollection<PermissionDefinition> GetPermissions() =>
    [
        new(TodosPermissions.ItemsRead, "View todos", TodosPermissions.Group),
        new(TodosPermissions.ItemsWrite, "Create and complete todos", TodosPermissions.Group)
    ];
}
