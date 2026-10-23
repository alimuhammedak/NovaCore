using CsCheck;
using NovaCore.Modules.Catalog.Domain;
using NovaCore.Modules.Payments.Domain;
using Xunit;

namespace NovaCore.Domain.Tests;

/// <summary>
/// The domain rules the reservation flow depends on, over generated sequences of operations (CsCheck) checked against a
/// small model: a payment settles once, from Pending (DECISIONS D-035 (4)); a product's stock follows what was taken and
/// never goes below zero.
/// </summary>
public sealed class DomainPropertyTests
{
    [Fact]
    public void A_payment_settles_once_and_repeating_its_outcome_changes_nothing()
    {
        // true completes, false fails; the first outcome decides.
        Gen.Bool.Array[1, 6].Sample(outcomes =>
        {
            var payment = new Payment("owner", Guid.NewGuid(), "Widget", 1, 10m);
            var decided = outcomes[0] ? PaymentStatus.Completed : PaymentStatus.Failed;

            foreach (var complete in outcomes)
            {
                var outcome = complete ? PaymentStatus.Completed : PaymentStatus.Failed;
                if (outcome == decided)
                    Settle(payment, complete);
                else
                    Assert.Throws<InvalidOperationException>(() => Settle(payment, complete));

                Assert.Equal(decided, payment.Status);
            }
        });
    }

    [Fact]
    public void A_payments_amount_is_its_unit_price_times_its_quantity()
    {
        Gen.Select(Gen.Int[1, 10_000], Gen.Long[1, 1_000_000_000_00]).Sample((quantity, cents) =>
        {
            var unitPrice = cents / 100m;

            Assert.Equal(unitPrice * quantity, new Payment("owner", Guid.NewGuid(), "Widget", quantity, unitPrice).Amount);
        });
    }

    [Fact]
    public void A_products_stock_follows_what_was_taken_and_never_goes_below_zero()
    {
        // A positive quantity takes stock; 0 deactivates the product, -1 reactivates it.
        Gen.Select(Gen.Int[0, 20], Gen.Int[-1, 8].Array[0, 12]).Sample((initialStock, operations) =>
        {
            var product = new Product("Widget", null, 10m, initialStock);
            var (stock, active) = (initialStock, true);

            foreach (var operation in operations)
            {
                switch (operation)
                {
                    case 0:
                        product.Deactivate();
                        active = false;
                        break;
                    case -1:
                        product.Reactivate();
                        active = true;
                        break;
                    default:
                        var taken = active && stock >= operation;
                        Assert.Equal(taken, product.RemoveStock(operation).IsSuccess);
                        if (taken)
                            stock -= operation;
                        break;
                }

                Assert.Equal(stock, product.Stock);
                Assert.InRange(product.Stock, 0, initialStock);
            }
        });
    }

    private static void Settle(Payment payment, bool complete)
    {
        if (complete)
            payment.Complete();
        else
            payment.Fail();
    }
}
