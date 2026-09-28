using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Domain;
using NovaCore.Modules.Catalog.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// PLAN F4b.2/F4b.3 (D-035 (3)): a product's stock is a concurrency token. A write over a stock that changed after it was
/// read fails instead of overwriting it, so a sale is never undone or counted twice. The product endpoints answer 409.
/// <see cref="StockRaceApiFactory"/> plays the other writer, between the read and the write.
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

    private static async Task AssertConflictAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("product.concurrency_conflict", problem.GetProperty("code").GetString());
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

    private async Task<int> StockAsync(Guid productId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Products
            .Where(product => product.Id == productId)
            .Select(product => product.Stock)
            .SingleAsync();
    }
}
