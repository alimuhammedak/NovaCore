using Microsoft.EntityFrameworkCore;
using NovaCore.Abstractions.Messaging;
using NovaCore.Abstractions.Results;
using NovaCore.Modules.Catalog.Domain;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Contracts;

namespace NovaCore.Modules.Catalog.Features;

/// <summary>
/// Vertical slice: the Catalog module's part in a payment. A <see cref="PaymentCreated"/> asks for stock; the product
/// gives it (<see cref="Product.RemoveStock"/>) or refuses. The answer is a <see cref="StockReservation"/>, saved in one
/// transaction with the stock change and with the event the outbox delivers to the Payments module.
/// </summary>
/// <remarks>
/// The outbox delivers a request at least once: a redelivered one finds its reservation and changes nothing.
/// </remarks>
internal sealed class ReserveStockForPayment(CatalogDbContext dbContext) : IDomainEventHandler<PaymentCreated>
{
    public async Task HandleAsync(PaymentCreated domainEvent, CancellationToken cancellationToken = default)
    {
        if (await dbContext.StockReservations.AnyAsync(r => r.Id == domainEvent.PaymentId, cancellationToken))
            return;

        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == domainEvent.ProductId, cancellationToken);
        var taken = product?.RemoveStock(domainEvent.Quantity)
            ?? Result.Failure(Error.NotFound("product.not_found", $"Product '{domainEvent.ProductId}' was not found."));

        dbContext.StockReservations.Add(taken.IsSuccess
            ? StockReservation.Reserve(domainEvent.PaymentId, domainEvent.ProductId, domainEvent.Quantity)
            : StockReservation.Refuse(domainEvent.PaymentId, domainEvent.ProductId, domainEvent.Quantity, taken.Error.Code));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
