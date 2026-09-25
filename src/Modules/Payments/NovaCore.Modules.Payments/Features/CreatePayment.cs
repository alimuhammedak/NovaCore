using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Http;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Validation;
using NovaCore.Modules.Catalog.Contracts;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Domain;
using NovaCore.Modules.Payments.Infrastructure;

namespace NovaCore.Modules.Payments.Features;

/// <summary>
/// Create a payment for a product. The cross-module showcase: Payments validates and prices the
/// product via <see cref="IProductCatalog"/> (an in-process contract) — never by touching Catalog's tables.
/// </summary>
public static class CreatePayment
{
    public sealed record Request(Guid ProductId, int Quantity);

    public sealed record Response(Guid PaymentId, decimal Amount, string Status);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }

    public static void Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreatePayment")
            .WithSummary("Create a payment for a product")
            .AddEndpointFilter<ValidationFilter<Request>>()
            .RequirePermission(PaymentsPermissions.Create)
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

    private static async Task<IResult> Handle(
        Request request,
        IProductCatalog catalog,
        PaymentsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var result = await CreateAsync(request, catalog, dbContext, cancellationToken);
        return result.ToHttpResult(success =>
            TypedResults.Created($"/api/v1/payments/{success.PaymentId}", success));
    }

    private static async Task<Result<Response>> CreateAsync(
        Request request,
        IProductCatalog catalog,
        PaymentsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var product = await catalog.GetProductAsync(request.ProductId, cancellationToken);

        if (product is null)
            return Error.NotFound("payment.product_not_found", $"Product '{request.ProductId}' was not found.");

        if (!product.IsActive)
            return Error.Validation("payment.product_inactive", $"Product '{product.Name}' is not available.");

        if (product.Stock < request.Quantity)
            return Error.Conflict("payment.insufficient_stock",
                $"Only {product.Stock} unit(s) of '{product.Name}' are in stock.");

        var payment = new Payment(product.Id, product.Name, request.Quantity, product.Price);
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new Response(payment.Id, payment.Amount, payment.Status.ToString());
    }
}
