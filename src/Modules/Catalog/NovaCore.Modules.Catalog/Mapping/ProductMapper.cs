using Riok.Mapperly.Abstractions;
using NovaCore.Modules.Catalog.Contracts;
using NovaCore.Modules.Catalog.Domain;

namespace NovaCore.Modules.Catalog.Mapping;

/// <summary>
/// Compile-time mapping via Mapperly (Apache-2.0, source-generated — no runtime reflection, fully
/// debuggable). Replaces AutoMapper, which is now a paid product.
/// </summary>
[Mapper]
public static partial class ProductMapper
{
    public static partial ProductDto ToDto(Product product);

    public static partial ProductInfo ToInfo(Product product);

    /// <summary>Source-generated, EF-translatable projection used for paged queries.</summary>
    public static partial IQueryable<ProductDto> ProjectToDto(this IQueryable<Product> source);
}
