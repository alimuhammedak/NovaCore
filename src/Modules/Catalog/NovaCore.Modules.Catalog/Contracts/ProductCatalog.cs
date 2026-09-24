using Microsoft.EntityFrameworkCore;
using NovaCore.Modules.Catalog.Contracts;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Catalog.Mapping;

namespace NovaCore.Modules.Catalog.Internal;

/// <summary>
/// In-process implementation of the Catalog module's public contract. Internal: other modules see
/// only <see cref="IProductCatalog"/>, never this type or the DbContext behind it.
/// </summary>
internal sealed class ProductCatalog(CatalogDbContext dbContext) : IProductCatalog
{
    public async Task<ProductInfo?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        return product is null ? null : ProductMapper.ToInfo(product);
    }
}
