using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Contracts;
using NovaCore.Modules.Payments.Domain;
using NovaCore.Modules.Payments.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// PLAN F4b.2/F4b.3 (D-035 (3)): stock never goes below zero and a unit is never sold twice, however the writes
/// interleave. A product's stock is a concurrency token: a write over a stock that changed after it was read fails
/// instead of overwriting it. The product endpoints then answer 409, and a reservation's delivery is retried by the
/// outbox on the new stock. <see cref="StockRaceApiFactory"/> plays the other writer, between the read and the write; the
/// payment tests run through the host's outbox dispatcher.
/// </summary>
public sealed class StockConcurrencyTests(StockRaceApiFactory factory) : IClassFixture<StockRaceApiFactory>
{
    [Fact]
    public async Task Updating_a_product_whose_stock_changed_after_it_was_read_is_a_conflict()
    {
        var productId = await factory.AddProductAsync(stock: 5);
        using var client = factory.CreateClientWithPermissions(CatalogPermissions.ProductsUpdate);
        factory.ChangeStockBeforeNextSave(productId, stock: 4);

        // A restock computed from the stock read before the sale would undo the sale.
        var response = await client.PutAsJsonAsync(
            $"/api/v1/products/{productId}", new { name = "Widget", price = 10m, stock = 8 });

        await AssertConflictAsync(response);
        Assert.Equal(4, await factory.StockAsync(productId));
    }

    [Fact]
    public async Task Deleting_a_product_whose_stock_changed_after_it_was_read_is_a_conflict()
    {
        var productId = await factory.AddProductAsync(stock: 5);
        using var client = factory.CreateClientWithPermissions(CatalogPermissions.ProductsDelete);
        factory.ChangeStockBeforeNextSave(productId, stock: 4);

        var response = await client.DeleteAsync($"/api/v1/products/{productId}");

        await AssertConflictAsync(response);
        Assert.Equal(4, await factory.StockAsync(productId));
    }

    [Fact]
    public async Task Two_payments_for_the_last_unit_sell_it_once()
    {
        var productId = await factory.AddProductAsync(stock: 1);

        // Both passed CreatePayment's check, as two requests for the last unit arriving together do; the reservation
        // decides. Saved directly, so a pass already running can't turn the second request away before it is saved.
        Guid[] paymentIds =
        [
            await factory.AddPaymentAsync(productId, quantity: 1),
            await factory.AddPaymentAsync(productId, quantity: 1),
        ];

        await OutboxDelivery.RunUntilAsync(factory.Clock, async () =>
            (await Task.WhenAll(paymentIds.Select(factory.PaymentStatusAsync))).All(status => status != PaymentStatus.Pending));

        var statuses = await Task.WhenAll(paymentIds.Select(factory.PaymentStatusAsync));
        Assert.Equal([PaymentStatus.Completed, PaymentStatus.Failed], statuses.Order());
        Assert.Equal(0, await factory.StockAsync(productId));
    }

    [Fact]
    public async Task A_reservation_over_stock_sold_after_it_was_read_is_retried_on_the_new_stock()
    {
        var productId = await factory.AddProductAsync(stock: 1);
        using var client = factory.CreateClientWithPermissions(PaymentsPermissions.Create);
        // Another sale takes the last unit while the reservation holds the stock it read.
        factory.ChangeStockBeforeNextSave(productId, stock: 0);

        var response = await client.PostAsJsonAsync("/api/v1/payments", new { productId, quantity = 1 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var paymentId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("paymentId").GetGuid();

        await OutboxDelivery.RunUntilAsync(factory.Clock,
            async () => await factory.PaymentStatusAsync(paymentId) != PaymentStatus.Pending);

        Assert.Equal(PaymentStatus.Failed, await factory.PaymentStatusAsync(paymentId));
        Assert.Equal(0, await factory.StockAsync(productId));
        var request = Assert.Single(await factory.OutboxMessagesAsync<PaymentsDbContext, PaymentCreated>(paymentId));
        Assert.Equal(1, request.Attempts);
        Assert.Contains(nameof(DbUpdateConcurrencyException), request.Error, StringComparison.Ordinal);
    }

    private static async Task AssertConflictAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("product.concurrency_conflict", problem.GetProperty("code").GetString());
    }
}
