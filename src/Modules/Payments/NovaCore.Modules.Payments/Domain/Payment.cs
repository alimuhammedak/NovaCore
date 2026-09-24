using NovaCore.Core.Abstractions.Entities;

namespace NovaCore.Modules.Payments.Domain;

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}

/// <summary>A payment for a quantity of a catalog product. Aggregate root of the Payments module.</summary>
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
    }

    public Guid ProductId { get; private set; }

    /// <summary>Snapshotted at purchase time — Payments never reads back across the Catalog boundary.</summary>
    public string ProductName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }

    public void Complete() => Status = PaymentStatus.Completed;
    public void Fail() => Status = PaymentStatus.Failed;
    public void Refund() => Status = PaymentStatus.Refunded;
}
