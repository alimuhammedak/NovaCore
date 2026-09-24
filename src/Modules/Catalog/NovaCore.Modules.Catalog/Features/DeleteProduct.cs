using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NovaCore.Core.Abstractions.Results;
using NovaCore.Core.Infrastructure.Http;
using NovaCore.Core.Web.Authorization;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Infrastructure;

namespace NovaCore.Modules.Catalog.Features;

/// <summary>Delete a product. Requires authorization; returns 204 on success, 404 if absent.</summary>
public static class DeleteProduct
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapDelete("/{id:guid}", Handle)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .RequirePermission(CatalogPermissions.ProductsDelete)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(
        Guid id,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
            return Result.Failure(Error.NotFound("product.not_found", $"Product '{id}' was not found.")).ToHttpResult();

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success().ToHttpResult();
    }
}
