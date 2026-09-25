using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NovaCore.Abstractions.Results;
using NovaCore.Core.Infrastructure.Http;
using NovaCore.Core.Web.Authorization;
using NovaCore.Core.Web.Validation;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Infrastructure;

namespace NovaCore.Modules.Catalog.Features;

/// <summary>Update an existing product. Write side — guarded by validation and authorization.</summary>
public static class UpdateProduct
{
    public sealed record Request(string Name, string? Description, decimal Price, int Stock);

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
        group.MapPut("/{id:guid}", Handle)
            .WithName("UpdateProduct")
            .WithSummary("Update a product")
            .AddEndpointFilter<ValidationFilter<Request>>()
            .RequirePermission(CatalogPermissions.ProductsUpdate)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

    private static async Task<IResult> Handle(
        Guid id,
        Request request,
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
            return Result.Failure(Error.NotFound("product.not_found", $"Product '{id}' was not found.")).ToHttpResult();

        product.Update(request.Name, request.Description, request.Price, request.Stock);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success().ToHttpResult();
    }
}
