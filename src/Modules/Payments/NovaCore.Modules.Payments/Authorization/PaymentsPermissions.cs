using NovaCore.AspNetCore.Authorization;

namespace NovaCore.Modules.Payments.Authorization;

/// <summary>Permissions owned by the Payments module. Format: <c>module:resource:action</c>.</summary>
public static class PaymentsPermissions
{
    public const string Group = "Payments";

    /// <summary>The caller's own payments.</summary>
    public const string Read = "payments:read";

    /// <summary>Every caller's payments, e.g. for support or accounting.</summary>
    public const string ReadAll = "payments:read-all";

    public const string Create = "payments:create";
}

public sealed class PaymentsPermissionProvider : IPermissionDefinitionProvider
{
    public IReadOnlyCollection<PermissionDefinition> GetPermissions() =>
    [
        new(PaymentsPermissions.Read, "View your own payments", PaymentsPermissions.Group),
        new(PaymentsPermissions.ReadAll, "View every user's payments", PaymentsPermissions.Group),
        new(PaymentsPermissions.Create, "Create payments", PaymentsPermissions.Group)
    ];
}
