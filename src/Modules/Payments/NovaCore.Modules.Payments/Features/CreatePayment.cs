using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NovaCore.Abstractions.Identity;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Http;
using NovaCore.AspNetCore.Authorization;
using NovaCore.Modules.Catalog.Contracts;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Domain;
using NovaCore.Modules.Payments.Infrastructure;

namespace NovaCore.Modules.Payments.Features;

/// <summary>
/// Create a payment for a product. The cross-module showcase: Payments checks and prices the product via
/// <see cref="IProductCatalog"/> (an in-process contract) — never by touching Catalog's tables — and saves the payment as
/// pending. Its <c>PaymentCreated</c> asks the Catalog module for the stock through the outbox, and the answer settles
/// the payment (<see cref="SettlePayment"/>). The checks here answer the common cases at once; the reservation is the
/// one that counts, so two requests for the last unit can both be accepted and one of them fail.
/// </summary>
public static class CreatePayment
{
    public sealed record Request([NotEmpty] Guid ProductId, [Range(1, int.MaxValue)] int Quantity);

    public sealed record Response(Guid PaymentId, decimal Amount, string Status);

    // [Required] can't reject Guid.Empty: a Guid is never null. A rule on a single value is a custom attribute; an
    // IValidatableObject would only run once every property is valid (docs/plan/RESEARCH.md R-052, Q-25).
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    private sealed class NotEmptyAttribute() : ValidationAttribute("The {0} field must not be empty.")
    {
        public override bool IsValid(object? value) => value is not Guid guid || guid != Guid.Empty;
    }

    public static void Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreatePayment")
            .WithSummary("Create a payment for a product")
            .RequirePermission(PaymentsPermissions.Create)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

    private static async Task<Results<Created<Response>, ProblemHttpResult>> Handle(
        Request request,
        IProductCatalog catalog,
        PaymentsDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        // The permission check passed, so the token has a subject: permissions are resolved per subject.
        var result = await CreateAsync(request, currentUser.UserId!, catalog, dbContext, cancellationToken);
        return result.ToHttpResult(success =>
            TypedResults.Created($"/api/v1/payments/{success.PaymentId}", success));
    }

    private static async Task<Result<Response>> CreateAsync(
        Request request,
        string ownerId,
        IProductCatalog catalog,
        PaymentsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var product = await catalog.GetProductAsync(request.ProductId, cancellationToken);

        if (product is null)
            return Result.Failure<Response>(Error.NotFound("payment.product_not_found", $"Product '{request.ProductId}' was not found."));

        if (!product.IsActive)
            return Result.Failure<Response>(
                Error.Validation("payment.product_inactive", $"Product '{product.Name}' is not available."));

        if (product.Stock < request.Quantity)
            return Result.Failure<Response>(Error.Conflict("payment.insufficient_stock",
                $"Only {product.Stock} unit(s) of '{product.Name}' are in stock."));

        var payment = new Payment(ownerId, product.Id, product.Name, request.Quantity, product.Price);
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new Response(payment.Id, payment.Amount, payment.Status.ToString()));
    }
}
