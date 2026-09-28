using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Messaging;
using NovaCore.Modules.Catalog.Contracts;
using NovaCore.Modules.Catalog.Domain;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Contracts;
using NovaCore.Modules.Payments.Domain;
using NovaCore.Modules.Payments.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// PLAN F4b.2 (D-034): the Payments side of the stock reservation. A created payment is saved with its
/// <see cref="PaymentCreated"/> in the Payments outbox; the Catalog module's answer settles it. The answers' handlers are
/// called here as the outbox dispatcher calls them. The fake clock never moves, so the dispatcher makes no pass after its
/// first.
/// </summary>
public sealed class PaymentSettlementTests(FakeClockApiFactory factory) : IClassFixture<FakeClockApiFactory>
{
    [Fact]
    public async Task A_created_payment_is_pending_and_its_PaymentCreated_waits_in_the_outbox()
    {
        var productId = await AddProductAsync(stock: 5);
        using var client = factory.CreateClientWithPermissions(PaymentsPermissions.Create);

        var response = await client.PostAsJsonAsync("/api/v1/payments", new { productId, quantity = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<CreatedPayment>();
        Assert.Equal("Pending", payment!.Status);
        Assert.Equal(1, await PaymentCreatedCountAsync(payment.PaymentId));
    }

    [Fact]
    public async Task Reserved_stock_completes_the_payment()
    {
        var paymentId = await AddPaymentAsync();

        await HandleAsync(new StockReserved(paymentId));

        Assert.Equal(PaymentStatus.Completed, await StatusAsync(paymentId));
    }

    [Fact]
    public async Task Refused_stock_fails_the_payment()
    {
        var paymentId = await AddPaymentAsync();

        await HandleAsync(new StockReservationFailed(paymentId));

        Assert.Equal(PaymentStatus.Failed, await StatusAsync(paymentId));
    }

    [Fact]
    public async Task A_redelivered_answer_changes_nothing()
    {
        var paymentId = await AddPaymentAsync();

        await HandleAsync(new StockReserved(paymentId));
        await HandleAsync(new StockReserved(paymentId));

        Assert.Equal(PaymentStatus.Completed, await StatusAsync(paymentId));
    }

    [Fact]
    public async Task An_answer_for_an_unknown_payment_fails_its_delivery()
    {
        // The dispatcher counts the failure and leaves the message for inspection once its attempts run out.
        await Assert.ThrowsAsync<InvalidOperationException>(() => HandleAsync(new StockReserved(Guid.CreateVersion7())));
    }

    private async Task<Guid> AddProductAsync(int stock)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = new Product("Widget", null, 10m, stock);
        catalog.Products.Add(product);
        await catalog.SaveChangesAsync();
        return product.Id;
    }

    // Saved the way CreatePayment saves it, so its PaymentCreated goes to the outbox too.
    private async Task<Guid> AddPaymentAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var payment = new Payment(Guid.CreateVersion7(), "Widget", 1, 10m);
        payments.Payments.Add(payment);
        await payments.SaveChangesAsync();
        return payment.Id;
    }

    private async Task HandleAsync<TEvent>(TEvent domainEvent)
        where TEvent : IDomainEvent
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IDomainEventHandler<TEvent>>().HandleAsync(domainEvent);
    }

    private async Task<PaymentStatus> StatusAsync(Guid paymentId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Payments
            .Where(payment => payment.Id == paymentId)
            .Select(payment => payment.Status)
            .SingleAsync();
    }

    private async Task<int> PaymentCreatedCountAsync(Guid paymentId)
    {
        var type = typeof(PaymentCreated);
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM outbox_messages WHERE Type = {0} AND Content LIKE {1}",
                $"{type.FullName}, {type.Assembly.GetName().Name}", $"%{paymentId}%")
            .SingleAsync();
    }

    private sealed record CreatedPayment(Guid PaymentId, decimal Amount, string Status);
}
