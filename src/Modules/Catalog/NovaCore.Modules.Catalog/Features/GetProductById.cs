using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Http;
using NovaCore.AspNetCore.Authorization;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Catalog.Mapping;

namespace NovaCore.Modules.Catalog.Features;

/// <summary>Fetch a single product. Demonstrates the Result -> ProblemDetails path for a 404.</summary>
public static class GetProductById
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/{id:guid}", Handle)
            .WithName("GetProductById")
            .WithSummary("Get a product by id")
            .RequirePermission(CatalogPermissions.ProductsRead)
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(
        Guid id,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        Result<ProductDto> result = product is null
            ? Error.NotFound("product.not_found", $"Product '{id}' was not found.")
            : ProductMapper.ToDto(product);

        return result.ToHttpResult();
    }
}
