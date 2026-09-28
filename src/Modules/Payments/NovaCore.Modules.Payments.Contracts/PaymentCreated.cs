using NovaCore.Abstractions.Messaging;

namespace NovaCore.Modules.Payments.Contracts;

/// <summary>
/// A payment was created and waits, pending, until its product's stock is reserved for it. Saved to the Payments outbox
/// with the payment; the answer names the payment.
/// </summary>
/// <param name="PaymentId">The payment.</param>
/// <param name="ProductId">The product whose stock the payment takes.</param>
/// <param name="Quantity">The units the payment takes.</param>
public sealed record PaymentCreated(Guid PaymentId, Guid ProductId, int Quantity) : DomainEvent;
