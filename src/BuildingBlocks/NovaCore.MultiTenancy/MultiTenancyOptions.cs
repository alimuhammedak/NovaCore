using Microsoft.AspNetCore.Http;

namespace NovaCore.MultiTenancy;

/// <summary>
/// Bound from the <c>MultiTenancy</c> section: the roles a membership can hold and the permissions each grants in its
/// tenant. Also how a request names its tenant (<c>AddCoreMultiTenancy</c>).
/// </summary>
internal sealed class MultiTenancyOptions
{
    public const string SectionName = "MultiTenancy";

    /// <summary>
    /// A role's name and the permissions it grants in the member's tenant, e.g. <c>MultiTenancy:Roles:Counselor</c> =
    /// [<c>crm:leads:read</c>, ...]. Each permission must be one a module declares. The product names its roles; there is
    /// no built-in role and no hierarchy (docs/plan/DECISIONS.md D-076 (7)).
    /// </summary>
    public Dictionary<string, string[]> Roles { get; } = new(StringComparer.Ordinal);

    /// <summary>The tenant identifier in the request, a Guid; anything else names no tenant.</summary>
    public Func<HttpContext, string?> TenantOf { get; set; } = _ => null;
}
