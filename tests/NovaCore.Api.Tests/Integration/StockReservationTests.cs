using System.Net;
using System.Net.Http.Json;
using NovaCore.Modules.Catalog.Contracts;
using NovaCore.Modules.Catalog.Domain;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Contracts;
using NovaCore.Modules.Payments.Domain;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// PLAN F4b.2 (D-034, D-035 (2)): the Catalog side of the stock reservation. A <see cref="PaymentCreated"/> takes the
/// payment's stock and is answered with <see cref="StockReserved"/>, or is refused, takes nothing and is answered with
/// <see cref="StockReservationFailed"/>. The answer and its <see cref="StockReservation"/> are saved with the stock change,
/// so a redelivered request changes nothing. The requests are handed to the handler here as the outbox dispatcher hands
/// them; the last test runs the whole flow through the dispatcher.
/// </summary>
public sealed class StockReservationTests(FakeClockApiFactory factory) : IClassFixture<FakeClockApiFactory>
{
    [Fact]
    public async Task Stock_that_suffices_is_reserved_and_the_answer_is_saved_with_it()
    {
        var productId = await factory.AddProductAsync(stock: 5);
        var request = await factory.AddPaymentForDirectHandlingAsync(productId, quantity: 2);

        await factory.HandleAsync(request);

        Assert.Equal(3, await factory.StockAsync(productId));
        var reservation = await factory.ReservationAsync(request.PaymentId);
        Assert.Equal(StockReservationStatus.Reserved, reservation.Status);
        Assert.Null(reservation.Reason);
        Assert.Single(await factory.OutboxMessagesAsync<CatalogDbContext, StockReserved>(request.PaymentId));
    }

    [Fact]
    public async Task A_missing_product_is_refused()
    {
        var request = await factory.AddPaymentForDirectHandlingAsync(Guid.CreateVersion7(), quantity: 1);

        await factory.HandleAsync(request);

        await AssertRefusedAsync(request, "product.not_found");
    }

    [Fact]
    public async Task An_inactive_product_is_refused_and_keeps_its_stock()
    {
        var productId = await factory.AddProductAsync(stock: 5, active: false);
        var request = await factory.AddPaymentForDirectHandlingAsync(productId, quantity: 1);

        await factory.HandleAsync(request);

        await AssertRefusedAsync(request, "product.inactive");
        Assert.Equal(5, await factory.StockAsync(productId));
    }

    [Fact]
    public async Task Stock_that_falls_short_is_refused_and_kept()
    {
        var productId = await factory.AddProductAsync(stock: 1);
        var request = await factory.AddPaymentForDirectHandlingAsync(productId, quantity: 2);

        await factory.HandleAsync(request);

        await AssertRefusedAsync(request, "product.insufficient_stock");
        Assert.Equal(1, await factory.StockAsync(productId));
    }

    [Fact]
    public async Task A_redelivered_request_changes_nothing()
    {
        var productId = await factory.AddProductAsync(stock: 5);
        var request = await factory.AddPaymentForDirectHandlingAsync(productId, quantity: 2);

        await factory.HandleAsync(request);
        await factory.HandleAsync(request);

        Assert.Equal(3, await factory.StockAsync(productId));
        Assert.Equal(StockReservationStatus.Reserved, (await factory.ReservationAsync(request.PaymentId)).Status);
        Assert.Single(await factory.OutboxMessagesAsync<CatalogDbContext, StockReserved>(request.PaymentId));
    }

    [Fact]
    public async Task A_payment_takes_its_stock_and_completes_through_the_outbox()
    {
        var productId = await factory.AddProductAsync(stock: 5);
        using var client = factory.CreateClientWithPermissions(PaymentsPermissions.Create);

        var response = await client.PostAsJsonAsync("/api/v1/payments", new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var paymentId = (await response.Content.ReadFromJsonAsync<CreatedPayment>())!.PaymentId;

        // One pass hands PaymentCreated to the Catalog module, the next one hands its answer to the Payments module.
        await OutboxDelivery.RunUntilAsync(factory.Clock,
            async () => await factory.PaymentStatusAsync(paymentId) == PaymentStatus.Completed);

        Assert.Equal(3, await factory.StockAsync(productId));
    }

    private async Task AssertRefusedAsync(PaymentCreated request, string reason)
    {
        var reservation = await factory.ReservationAsync(request.PaymentId);
        Assert.Equal(StockReservationStatus.Refused, reservation.Status);
        Assert.Equal(reason, reservation.Reason);
        Assert.Single(await factory.OutboxMessagesAsync<CatalogDbContext, StockReservationFailed>(request.PaymentId));
    }

    private sealed record CreatedPayment(Guid PaymentId, decimal Amount, string Status);
}
