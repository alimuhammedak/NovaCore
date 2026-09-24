using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NovaCore.Core.Web.Authorization;
using NovaCore.Core.Web.Validation;
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
    public sealed record Request(string Name, string? Description, decimal Price, int Stock);

    public sealed record Response(Guid Id);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Price).GreaterThan(0);
            RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        }
    }

    public static void Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreateProduct")
            .WithSummary("Create a new product")
            .AddEndpointFilter<ValidationFilter<Request>>()
            .RequirePermission(CatalogPermissions.ProductsCreate)
            .Produces<Response>(StatusCodes.Status201Created)
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
