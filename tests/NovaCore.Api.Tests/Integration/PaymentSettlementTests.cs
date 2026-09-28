using System.Net;
using System.Net.Http.Json;
using NovaCore.Modules.Catalog.Contracts;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Contracts;
using NovaCore.Modules.Payments.Domain;
using NovaCore.Modules.Payments.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// PLAN F4b.2 (D-034): the Payments side of the stock reservation. A created payment is saved with its
/// <see cref="PaymentCreated"/> in the Payments outbox; the Catalog module's answer settles it. The answers are handed to
/// their handlers here as the outbox dispatcher hands them, for payments saved without their request in the outbox.
/// </summary>
public sealed class PaymentSettlementTests(FakeClockApiFactory factory) : IClassFixture<FakeClockApiFactory>
{
    [Fact]
    public async Task A_created_payment_is_pending_and_its_PaymentCreated_is_saved_to_the_outbox()
    {
        var productId = await factory.AddProductAsync(stock: 5);
        using var client = factory.CreateClientWithPermissions(PaymentsPermissions.Create);

        var response = await client.PostAsJsonAsync("/api/v1/payments", new { productId, quantity = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<CreatedPayment>();
        Assert.Equal("Pending", payment!.Status);
        Assert.Single(await factory.OutboxMessagesAsync<PaymentsDbContext, PaymentCreated>(payment.PaymentId));
    }

    [Fact]
    public async Task Reserved_stock_completes_the_payment()
    {
        var paymentId = await AddPaymentAsync();

        await factory.HandleAsync(new StockReserved(paymentId));

        Assert.Equal(PaymentStatus.Completed, await factory.PaymentStatusAsync(paymentId));
    }

    [Fact]
    public async Task Refused_stock_fails_the_payment()
    {
        var paymentId = await AddPaymentAsync();

        await factory.HandleAsync(new StockReservationFailed(paymentId));

        Assert.Equal(PaymentStatus.Failed, await factory.PaymentStatusAsync(paymentId));
    }

    [Fact]
    public async Task A_redelivered_answer_changes_nothing()
    {
        var paymentId = await AddPaymentAsync();

        await factory.HandleAsync(new StockReserved(paymentId));
        await factory.HandleAsync(new StockReserved(paymentId));

        Assert.Equal(PaymentStatus.Completed, await factory.PaymentStatusAsync(paymentId));
    }

    [Fact]
    public async Task An_answer_for_an_unknown_payment_fails_its_delivery()
    {
        // The dispatcher counts the failure and leaves the message for inspection once its attempts run out.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.HandleAsync(new StockReserved(Guid.CreateVersion7())));
    }

    private async Task<Guid> AddPaymentAsync() =>
        (await factory.AddPaymentForDirectHandlingAsync(Guid.CreateVersion7(), quantity: 1)).PaymentId;

    private sealed record CreatedPayment(Guid PaymentId, decimal Amount, string Status);
}
