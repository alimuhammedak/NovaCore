using NovaCore.Abstractions.Messaging;

namespace NovaCore.Modules.Catalog.Contracts;

/// <summary>
/// The stock a payment asked for (the Payments module's <c>PaymentCreated</c>) was taken from its product. Saved to the
/// Catalog outbox with the stock change.
/// </summary>
/// <param name="PaymentId">The payment the stock was reserved for.</param>
public sealed record StockReserved(Guid PaymentId) : DomainEvent;

/// <summary>
/// The stock a payment asked for could not be reserved: the product is gone, inactive or short of stock. Nothing was
/// taken.
/// </summary>
/// <param name="PaymentId">The payment whose stock was refused.</param>
public sealed record StockReservationFailed(Guid PaymentId) : DomainEvent;
