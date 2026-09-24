namespace NovaCore.Modules.Catalog.Contracts;

/// <summary>
/// The Catalog module's public, in-process API. Other modules call this instead of touching the
/// Catalog database — keeping the boundary enforceable and an eventual service split cheap.
/// </summary>
public interface IProductCatalog
{
    /// <summary>Returns lightweight product info, or <c>null</c> if no such product exists.</summary>
    Task<ProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
}

/// <summary>A minimal, stable projection of a product for cross-module consumers.</summary>
public sealed record ProductInfo(Guid Id, string Name, decimal Price, int Stock, bool IsActive);
