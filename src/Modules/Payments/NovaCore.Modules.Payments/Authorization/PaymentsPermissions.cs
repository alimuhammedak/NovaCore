using NovaCore.AspNetCore.Authorization;

namespace NovaCore.Modules.Payments.Authorization;

/// <summary>Permissions owned by the Payments module. Format: <c>module:resource:action</c>.</summary>
public static class PaymentsPermissions
{
    public const string Group = "Payments";

    public const string Read = "payments:read";
    public const string Create = "payments:create";
}

public sealed class PaymentsPermissionProvider : IPermissionDefinitionProvider
{
    public IReadOnlyCollection<PermissionDefinition> GetPermissions() =>
    [
        new(PaymentsPermissions.Read, "View payments", PaymentsPermissions.Group),
        new(PaymentsPermissions.Create, "Create payments", PaymentsPermissions.Group)
    ];
}
