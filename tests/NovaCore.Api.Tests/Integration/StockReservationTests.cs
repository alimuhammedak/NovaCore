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
/// PLAN F4b.2 (D-034, D-035 (2)): the Catalog side of the stock reservation. A <see cref="PaymentCreated"/> takes the
/// payment's stock and is answered with <see cref="StockReserved"/>, or is refused, takes nothing and is answered with
/// <see cref="StockReservationFailed"/>. The answer and its <see cref="StockReservation"/> are saved with the stock change,
/// so a redelivered request changes nothing. The handler is called here as the outbox dispatcher calls it; the last test
/// runs the whole flow through the dispatcher.
/// </summary>
public sealed class StockReservationTests(FakeClockApiFactory factory) : IClassFixture<FakeClockApiFactory>
{
    [Fact]
    public async Task Stock_that_suffices_is_reserved_and_the_answer_is_saved_with_it()
    {
        var productId = await AddProductAsync(stock: 5);
        var request = await AddPaymentAsync(productId, quantity: 2);

        await HandleAsync(request);

        Assert.Equal(3, await StockAsync(productId));
        var reservation = await ReservationAsync(request.PaymentId);
        Assert.Equal(StockReservationStatus.Reserved, reservation.Status);
        Assert.Null(reservation.Reason);
        Assert.Equal(1, await AnswerCountAsync<StockReserved>(request.PaymentId));
    }

    [Fact]
    public async Task A_missing_product_is_refused()
    {
        var request = await AddPaymentAsync(Guid.CreateVersion7(), quantity: 1);

        await HandleAsync(request);

        await AssertRefusedAsync(request, "product.not_found");
    }

    [Fact]
    public async Task An_inactive_product_is_refused_and_keeps_its_stock()
    {
        var productId = await AddProductAsync(stock: 5, active: false);
        var request = await AddPaymentAsync(productId, quantity: 1);

        await HandleAsync(request);

        await AssertRefusedAsync(request, "product.inactive");
        Assert.Equal(5, await StockAsync(productId));
    }

    [Fact]
    public async Task Stock_that_falls_short_is_refused_and_kept()
    {
        var productId = await AddProductAsync(stock: 1);
        var request = await AddPaymentAsync(productId, quantity: 2);

        await HandleAsync(request);

        await AssertRefusedAsync(request, "product.insufficient_stock");
        Assert.Equal(1, await StockAsync(productId));
    }

    [Fact]
    public async Task A_redelivered_request_changes_nothing()
    {
        var productId = await AddProductAsync(stock: 5);
        var request = await AddPaymentAsync(productId, quantity: 2);

        await HandleAsync(request);
        await HandleAsync(request);

        Assert.Equal(3, await StockAsync(productId));
        Assert.Equal(StockReservationStatus.Reserved, (await ReservationAsync(request.PaymentId)).Status);
        Assert.Equal(1, await AnswerCountAsync<StockReserved>(request.PaymentId));
    }

    [Fact]
    public async Task A_payment_takes_its_stock_and_completes_through_the_outbox()
    {
        var productId = await AddProductAsync(stock: 5);
        using var client = factory.CreateClientWithPermissions(PaymentsPermissions.Create);

        var response = await client.PostAsJsonAsync("/api/v1/payments", new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var paymentId = (await response.Content.ReadFromJsonAsync<CreatedPayment>())!.PaymentId;

        // One pass hands PaymentCreated to the Catalog module, the next one hands its answer to the Payments module.
        await OutboxDelivery.RunUntilAsync(factory.Clock,
            async () => await PaymentStatusAsync(paymentId) == PaymentStatus.Completed);

        Assert.Equal(3, await StockAsync(productId));
    }

    private async Task AssertRefusedAsync(PaymentCreated request, string reason)
    {
        var reservation = await ReservationAsync(request.PaymentId);
        Assert.Equal(StockReservationStatus.Refused, reservation.Status);
        Assert.Equal(reason, reservation.Reason);
        Assert.Equal(1, await AnswerCountAsync<StockReservationFailed>(request.PaymentId));
    }

    private async Task<Guid> AddProductAsync(int stock, bool active = true)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = new Product("Widget", null, 10m, stock);
        if (!active)
            product.Deactivate();
        catalog.Products.Add(product);
        await catalog.SaveChangesAsync();
        return product.Id;
    }

    // The request comes back to be handed to the handler by the test alone: kept out of the outbox, the dispatcher never
    // delivers it at the same time.
    private async Task<PaymentCreated> AddPaymentAsync(Guid productId, int quantity)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var payment = new Payment(productId, "Widget", quantity, 10m);
        var request = Assert.IsType<PaymentCreated>(Assert.Single(payment.DomainEvents));
        payment.ClearDomainEvents();
        payments.Payments.Add(payment);
        await payments.SaveChangesAsync();
        return request;
    }

    private async Task HandleAsync(PaymentCreated request)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IDomainEventHandler<PaymentCreated>>().HandleAsync(request);
    }

    private async Task<int> StockAsync(Guid productId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Products
            .Where(product => product.Id == productId)
            .Select(product => product.Stock)
            .SingleAsync();
    }

    private async Task<StockReservation> ReservationAsync(Guid paymentId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().StockReservations
            .AsNoTracking()
            .SingleAsync(reservation => reservation.Id == paymentId);
    }

    // The answers the Catalog module saved for the payment, waiting in its outbox or delivered.
    private async Task<int> AnswerCountAsync<TAnswer>(Guid paymentId)
        where TAnswer : IDomainEvent
    {
        var type = typeof(TAnswer);
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM outbox_messages WHERE Type = {0} AND Content LIKE {1}",
                $"{type.FullName}, {type.Assembly.GetName().Name}", $"%{paymentId}%")
            .SingleAsync();
    }

    // From the database: the payment list orders by a DateTimeOffset, and the SQLite test database can't order by one.
    private async Task<PaymentStatus> PaymentStatusAsync(Guid paymentId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Payments
            .Where(payment => payment.Id == paymentId)
            .Select(payment => payment.Status)
            .SingleAsync();
    }

    private sealed record CreatedPayment(Guid PaymentId, decimal Amount, string Status);
}
