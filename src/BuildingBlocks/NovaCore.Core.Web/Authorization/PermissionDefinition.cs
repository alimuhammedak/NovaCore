namespace NovaCore.Core.Web.Authorization;

/// <summary>A permission a module exposes, e.g. <c>catalog:products:create</c>.</summary>
public sealed record PermissionDefinition(string Name, string DisplayName, string Group);

/// <summary>
/// Modules implement this to declare the permissions they own. The Identity seeder collects every
/// provider from DI and seeds roles/claims — so a module stays authoritative over its own permissions
/// without any central permission list to keep in sync.
/// </summary>
public interface IPermissionDefinitionProvider
{
    /// <summary>The permissions this module owns.</summary>
    IReadOnlyCollection<PermissionDefinition> GetPermissions();
}
