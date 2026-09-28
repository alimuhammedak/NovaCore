using NovaCore.Abstractions.Entities;
using NovaCore.Modules.Catalog.Contracts;

namespace NovaCore.Modules.Catalog.Domain;

public enum StockReservationStatus
{
    Reserved = 0,
    Refused = 1
}

/// <summary>
/// The Catalog module's answer to one payment's request for stock, keyed by the payment. It is saved with the stock change
/// and raises the answer, so a redelivered request finds it and changes nothing.
/// </summary>
public sealed class StockReservation : AuditableEntity<Guid>
{
    // EF Core materialization ctor.
    private StockReservation() { }

    private StockReservation(Guid paymentId, Guid productId, int quantity, StockReservationStatus status, string? reason)
        : base(paymentId)
    {
        ProductId = productId;
        Quantity = quantity;
        Status = status;
        Reason = reason;
    }

    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public StockReservationStatus Status { get; private set; }

    /// <summary>Why the stock was refused (an error code such as <c>product.insufficient_stock</c>); null when reserved.</summary>
    public string? Reason { get; private set; }

    /// <summary>The stock was taken from the product: answers the payment with <see cref="StockReserved"/>.</summary>
    public static StockReservation Reserve(Guid paymentId, Guid productId, int quantity)
    {
        var reservation = new StockReservation(paymentId, productId, quantity, StockReservationStatus.Reserved, reason: null);
        reservation.Raise(new StockReserved(paymentId));
        return reservation;
    }

    /// <summary>Nothing was taken: answers the payment with <see cref="StockReservationFailed"/>.</summary>
    public static StockReservation Refuse(Guid paymentId, Guid productId, int quantity, string reason)
    {
        var reservation = new StockReservation(paymentId, productId, quantity, StockReservationStatus.Refused, reason);
        reservation.Raise(new StockReservationFailed(paymentId));
        return reservation;
    }
}
