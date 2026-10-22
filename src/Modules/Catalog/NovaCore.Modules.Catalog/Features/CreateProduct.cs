using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NovaCore.AspNetCore.Authorization;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Domain;
using NovaCore.Modules.Catalog.Infrastructure;

namespace NovaCore.Modules.Catalog.Features;

/// <summary>
/// Vertical slice: request, response, validation, route and handler for "create a product" — all in
/// one file. No mediator; the handler receives its dependencies directly from minimal-API DI.
/// </summary>
public static class CreateProduct
{
    public sealed record Request(
        [Required, MaxLength(200)] string Name,
        [MaxLength(2000)] string? Description,
        [Range(0d, double.MaxValue, MinimumIsExclusive = true, ErrorMessage = "The field {0} must be greater than 0.")]
        [DecimalColumn(ProductConfiguration.PricePrecision, ProductConfiguration.PriceScale)]
        decimal Price,
        [Range(0, int.MaxValue)] int Stock);

    public sealed record Response(Guid Id);

    public static void Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreateProduct")
            .WithSummary("Create a new product")
            .RequirePermission(CatalogPermissions.ProductsCreate)
            .ProducesValidationProblem();

    private static async Task<Created<Response>> Handle(
        Request request,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var product = new Product(request.Name, request.Description, request.Price, request.Stock);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/v1/products/{product.Id}", new Response(product.Id));
    }
}
