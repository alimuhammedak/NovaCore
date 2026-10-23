using CsCheck;
using NovaCore.Modules.Catalog.Domain;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// Catalog's stock reservation handler under at-least-once delivery (CsCheck): however the requests for one product
/// interleave and however often each comes again, each payment is decided once, by its first delivery, and the stock
/// moves by exactly the quantities reserved. Each sample uses a product and payments of its own on the shared host,
/// handing the requests to the handler as the outbox dispatcher does (<c>HandleAsync</c>).
/// </summary>
public sealed class StockReservationPropertyTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    [Fact]
    public async Task Each_payment_is_decided_once_whatever_the_redeliveries()
    {
        // An initial stock, the quantities of the payments, and the order of deliveries (as indexes into the payments,
        // repeats included).
        var samples = Gen.Select(Gen.Int[0, 10], Gen.Int[1, 4].Array[1, 5], Gen.Int[0, 99].Array[1, 12]);

        await samples.SampleAsync(async sample =>
        {
            var (initialStock, quantities, deliveries) = sample;
            var productId = await factory.AddProductAsync(initialStock);
            var requests = new List<Modules.Payments.Contracts.PaymentCreated>();
            foreach (var quantity in quantities)
                requests.Add(await factory.AddPaymentForDirectHandlingAsync(productId, quantity));

            var stock = initialStock;
            var decided = new Dictionary<int, StockReservationStatus>();
            foreach (var delivery in deliveries)
            {
                var index = delivery % requests.Count;
                await factory.HandleAsync(requests[index]);

                if (!decided.ContainsKey(index))
                {
                    var reserved = stock >= quantities[index];
                    decided[index] = reserved ? StockReservationStatus.Reserved : StockReservationStatus.Refused;
                    if (reserved)
                        stock -= quantities[index];
                }
            }

            Assert.Equal(stock, await factory.StockAsync(productId));
            foreach (var (index, status) in decided)
                Assert.Equal(status, (await factory.ReservationAsync(requests[index].PaymentId)).Status);
        }, iter: 25, threads: 1);
    }
}
