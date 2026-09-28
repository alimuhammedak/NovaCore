using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Domain;
using NovaCore.Modules.Catalog.Infrastructure;
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
        var productId = await AddProductAsync(stock: 5);
        using var client = factory.CreateClientWithPermissions(CatalogPermissions.ProductsUpdate);
        factory.ChangeStockBeforeNextSave(productId, stock: 4);

        // A restock computed from the stock read before the sale would undo the sale.
        var response = await client.PutAsJsonAsync(
            $"/api/v1/products/{productId}", new { name = "Widget", price = 10m, stock = 8 });

        await AssertConflictAsync(response);
        Assert.Equal(4, await StockAsync(productId));
    }

    [Fact]
    public async Task Deleting_a_product_whose_stock_changed_after_it_was_read_is_a_conflict()
    {
        var productId = await AddProductAsync(stock: 5);
        using var client = factory.CreateClientWithPermissions(CatalogPermissions.ProductsDelete);
        factory.ChangeStockBeforeNextSave(productId, stock: 4);

        var response = await client.DeleteAsync($"/api/v1/products/{productId}");

        await AssertConflictAsync(response);
        Assert.Equal(4, await StockAsync(productId));
    }

    [Fact]
    public async Task Two_payments_for_the_last_unit_sell_it_once()
    {
        var productId = await AddProductAsync(stock: 1);
        using var client = factory.CreateClientWithPermissions(PaymentsPermissions.Create);

        // Both pass CreatePayment's check; the reservation decides.
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v1/payments", new { productId, quantity = 1 }),
            client.PostAsJsonAsync("/api/v1/payments", new { productId, quantity = 1 }));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        var paymentIds = await Task.WhenAll(responses.Select(PaymentIdAsync));

        await OutboxDelivery.RunUntilAsync(factory.Clock, async () =>
            (await Task.WhenAll(paymentIds.Select(PaymentStatusAsync))).All(status => status != PaymentStatus.Pending));

        var statuses = await Task.WhenAll(paymentIds.Select(PaymentStatusAsync));
        Assert.Equal([PaymentStatus.Completed, PaymentStatus.Failed], statuses.Order());
        Assert.Equal(0, await StockAsync(productId));
    }

    [Fact]
    public async Task A_reservation_over_stock_sold_after_it_was_read_is_retried_on_the_new_stock()
    {
        var productId = await AddProductAsync(stock: 1);
        using var client = factory.CreateClientWithPermissions(PaymentsPermissions.Create);
        // Another sale takes the last unit while the reservation holds the stock it read.
        factory.ChangeStockBeforeNextSave(productId, stock: 0);

        var response = await client.PostAsJsonAsync("/api/v1/payments", new { productId, quantity = 1 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var paymentId = await PaymentIdAsync(response);

        await OutboxDelivery.RunUntilAsync(factory.Clock,
            async () => await PaymentStatusAsync(paymentId) != PaymentStatus.Pending);

        Assert.Equal(PaymentStatus.Failed, await PaymentStatusAsync(paymentId));
        Assert.Equal(0, await StockAsync(productId));
        var request = await PaymentCreatedMessageAsync(paymentId);
        Assert.Equal(1, request.Attempts);
        Assert.Contains(nameof(DbUpdateConcurrencyException), request.Error, StringComparison.Ordinal);
    }

    private static async Task AssertConflictAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("product.concurrency_conflict", problem.GetProperty("code").GetString());
    }

    private static async Task<Guid> PaymentIdAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("paymentId").GetGuid();

    private async Task<Guid> AddProductAsync(int stock)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = new Product("Widget", null, 10m, stock);
        catalog.Products.Add(product);
        await catalog.SaveChangesAsync();
        return product.Id;
    }

    private async Task<int> StockAsync(Guid productId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Products
            .Where(product => product.Id == productId)
            .Select(product => product.Stock)
            .SingleAsync();
    }

    private async Task<PaymentStatus> PaymentStatusAsync(Guid paymentId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Payments
            .Where(payment => payment.Id == paymentId)
            .Select(payment => payment.Status)
            .SingleAsync();
    }

    private async Task<OutboxRow> PaymentCreatedMessageAsync(Guid paymentId)
    {
        var type = typeof(PaymentCreated);
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database
            .SqlQueryRaw<OutboxRow>(
                "SELECT Attempts, Error FROM outbox_messages WHERE Type = {0} AND Content LIKE {1}",
                $"{type.FullName}, {type.Assembly.GetName().Name}", $"%{paymentId}%")
            .SingleAsync();
    }

    private sealed record OutboxRow(int Attempts, string? Error);
}
