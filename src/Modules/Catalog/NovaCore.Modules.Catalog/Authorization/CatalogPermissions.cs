using NovaCore.AspNetCore.Authorization;

namespace NovaCore.Modules.Catalog.Authorization;

/// <summary>Permissions owned by the Catalog module. Format: <c>module:resource:action</c>.</summary>
public static class CatalogPermissions
{
    public const string Group = "Catalog";

    public const string ProductsRead = "catalog:products:read";
    public const string ProductsCreate = "catalog:products:create";
    public const string ProductsUpdate = "catalog:products:update";
    public const string ProductsDelete = "catalog:products:delete";
}

public sealed class CatalogPermissionProvider : IPermissionDefinitionProvider
{
    public IReadOnlyCollection<PermissionDefinition> GetPermissions() =>
    [
        new(CatalogPermissions.ProductsRead, "View products", CatalogPermissions.Group),
        new(CatalogPermissions.ProductsCreate, "Create products", CatalogPermissions.Group),
        new(CatalogPermissions.ProductsUpdate, "Update products", CatalogPermissions.Group),
        new(CatalogPermissions.ProductsDelete, "Delete products", CatalogPermissions.Group)
    ];
}
