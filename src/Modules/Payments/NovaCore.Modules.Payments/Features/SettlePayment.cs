using Microsoft.EntityFrameworkCore;
using NovaCore.Abstractions.Messaging;
using NovaCore.Modules.Catalog.Contracts;
using NovaCore.Modules.Payments.Domain;
using NovaCore.Modules.Payments.Infrastructure;

namespace NovaCore.Modules.Payments.Features;

/// <summary>
/// Vertical slice: a pending payment settles when the Catalog module answers its <c>PaymentCreated</c>.
/// <see cref="StockReserved"/> completes it, <see cref="StockReservationFailed"/> fails it. The outbox delivers each answer
/// at least once; the payment ignores the same outcome again (<see cref="Payment.Complete"/>).
/// </summary>
internal static class SettlePayment
{
    internal sealed class WhenStockReserved(PaymentsDbContext dbContext) : IDomainEventHandler<StockReserved>
    {
        public Task HandleAsync(StockReserved domainEvent, CancellationToken cancellationToken = default) =>
            SettleAsync(dbContext, domainEvent.PaymentId, payment => payment.Complete(), cancellationToken);
    }

    internal sealed class WhenStockReservationFailed(PaymentsDbContext dbContext)
        : IDomainEventHandler<StockReservationFailed>
    {
        public Task HandleAsync(StockReservationFailed domainEvent, CancellationToken cancellationToken = default) =>
            SettleAsync(dbContext, domainEvent.PaymentId, payment => payment.Fail(), cancellationToken);
    }

    private static async Task SettleAsync(
        PaymentsDbContext dbContext, Guid paymentId, Action<Payment> settle, CancellationToken cancellationToken)
    {
        // The answer names a payment this module saved, so a missing one is a bug: the delivery fails, and the message is
        // left for inspection once its attempts run out.
        var payment = await dbContext.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new InvalidOperationException($"Payment '{paymentId}' does not exist.");

        settle(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
