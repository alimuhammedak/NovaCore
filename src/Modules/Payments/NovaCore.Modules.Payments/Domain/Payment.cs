using NovaCore.Abstractions.Entities;
using NovaCore.Modules.Payments.Contracts;

namespace NovaCore.Modules.Payments.Domain;

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2
}

/// <summary>
/// A payment for a quantity of a catalog product. Aggregate root of the Payments module. It starts pending and asks for its
/// stock (<see cref="PaymentCreated"/>); the Catalog module's answer settles it, once.
/// </summary>
public sealed class Payment : AuditableEntity<Guid>
{
    private Payment() { }

    public Payment(Guid productId, string productName, int quantity, decimal unitPrice)
        : base(Guid.CreateVersion7())
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Amount = unitPrice * quantity;
        Status = PaymentStatus.Pending;
        Raise(new PaymentCreated(Id, productId, quantity));
    }

    public Guid ProductId { get; private set; }

    /// <summary>Snapshotted at purchase time — Payments never reads back across the Catalog boundary.</summary>
    public string ProductName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }

    /// <summary>The stock was reserved: the payment is done.</summary>
    public void Complete() => Settle(PaymentStatus.Completed);

    /// <summary>The stock could not be reserved: the payment failed.</summary>
    public void Fail() => Settle(PaymentStatus.Failed);

    // A payment settles once, from Pending. The outbox delivers an answer at least once, so the same outcome again changes
    // nothing; the other outcome would contradict the first.
    private void Settle(PaymentStatus outcome)
    {
        if (Status == outcome)
            return;
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Payment '{Id}' is already {Status} and cannot become {outcome}.");

        Status = outcome;
    }
}
