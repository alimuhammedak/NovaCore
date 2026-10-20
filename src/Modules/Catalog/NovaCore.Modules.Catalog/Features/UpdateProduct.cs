using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Http;
using NovaCore.AspNetCore.Authorization;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Infrastructure;

namespace NovaCore.Modules.Catalog.Features;

/// <summary>Update an existing product. Write side — guarded by validation and authorization.</summary>
public static class UpdateProduct
{
    public sealed record Request(
        [Required, MaxLength(200)] string Name,
        [MaxLength(2000)] string? Description,
        [Range(0d, double.MaxValue, MinimumIsExclusive = true, ErrorMessage = "The field {0} must be greater than 0.")]
        decimal Price,
        [Range(0, int.MaxValue)] int Stock);

    public static void Map(IEndpointRouteBuilder group) =>
        group.MapPut("/{id:guid}", Handle)
            .WithName("UpdateProduct")
            .WithSummary("Update a product")
            .RequirePermission(CatalogPermissions.ProductsUpdate)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        Guid id,
        Request request,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
            return Result.Failure(Error.NotFound("product.not_found", $"Product '{id}' was not found.")).ToHttpResult();

        product.Update(request.Name, request.Description, request.Price, request.Stock);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A sale took stock after the product was read (Stock is a concurrency token): writing the requested stock
            // over it would undo the sale. The caller reads the product again and decides.
            return Result.Failure(Error.Conflict("product.concurrency_conflict",
                $"Product '{id}' changed while it was being updated. Read it again and retry.")).ToHttpResult();
        }

        return Result.Success().ToHttpResult();
    }
}
