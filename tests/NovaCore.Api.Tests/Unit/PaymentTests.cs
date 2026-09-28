using NovaCore.Modules.Payments.Contracts;
using NovaCore.Modules.Payments.Domain;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// PLAN F4b.2 (D-034, D-035 (4)): a payment starts pending and asks for its stock with <see cref="PaymentCreated"/>. The
/// Catalog module's answer settles it once, as Completed or Failed. The outbox delivers the answer at least once, so the
/// same outcome again changes nothing, and a settled payment keeps its outcome.
/// </summary>
public class PaymentTests
{
    private static readonly Guid ProductId = Guid.CreateVersion7();

    [Fact]
    public void A_new_payment_is_pending_and_asks_for_its_stock()
    {
        var payment = new Payment(ProductId, "Widget", 2, 9.99m);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(19.98m, payment.Amount);
        var created = Assert.IsType<PaymentCreated>(Assert.Single(payment.DomainEvents));
        Assert.Equal((payment.Id, ProductId, 2), (created.PaymentId, created.ProductId, created.Quantity));
    }

    [Fact]
    public void Reserved_stock_completes_a_pending_payment()
    {
        var payment = NewPayment();

        payment.Complete();

        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    [Fact]
    public void Refused_stock_fails_a_pending_payment()
    {
        var payment = NewPayment();

        payment.Fail();

        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }

    [Fact]
    public void The_same_outcome_again_changes_nothing()
    {
        var completed = NewPayment();
        var failed = NewPayment();

        completed.Complete();
        completed.Complete();
        failed.Fail();
        failed.Fail();

        Assert.Equal(PaymentStatus.Completed, completed.Status);
        Assert.Equal(PaymentStatus.Failed, failed.Status);
    }

    [Fact]
    public void A_settled_payment_keeps_its_outcome()
    {
        var completed = NewPayment();
        var failed = NewPayment();
        completed.Complete();
        failed.Fail();

        Assert.Throws<InvalidOperationException>(completed.Fail);
        Assert.Throws<InvalidOperationException>(failed.Complete);
        Assert.Equal(PaymentStatus.Completed, completed.Status);
        Assert.Equal(PaymentStatus.Failed, failed.Status);
    }

    private static Payment NewPayment() => new(ProductId, "Widget", 1, 5m);
}
