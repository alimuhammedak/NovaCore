using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NovaCore.Modules.Catalog.Authorization;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// PLAN F5.3 (moved to 4b, D-033): the Catalog endpoints with valid bodies, end to end. <see cref="ValidationTests"/> pins
/// the rejected bodies; these show that valid ones are accepted and do what they say. Paying for a product runs end to end
/// in <see cref="StockReservationTests"/>.
/// </summary>
public sealed class CatalogEndpointsTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    [Fact]
    public async Task A_created_product_can_be_read_and_is_listed()
    {
        using var client = CatalogClient();
        var name = UniqueName();

        var created = await client.PostAsJsonAsync("/api/v1/products",
            new { name, description = "A test widget", price = 9.99m, stock = 5 });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal($"/api/v1/products/{id}", created.Headers.Location?.OriginalString);

        var product = await client.GetFromJsonAsync<ProductBody>(created.Headers.Location);
        Assert.Equal(new ProductBody(id, name, "A test widget", 9.99m, 5, IsActive: true), product);

        var page = await client.GetFromJsonAsync<ProductPage>($"/api/v1/products?search={name}");
        Assert.Equal([id], page!.Items.Select(item => item.Id));
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task An_updated_product_reads_back_with_its_new_values()
    {
        using var client = CatalogClient();
        var id = await CreateProductAsync(client);
        var name = UniqueName();

        var updated = await client.PutAsJsonAsync($"/api/v1/products/{id}",
            new { name, description = (string?)null, price = 12.50m, stock = 8 });

        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var product = await client.GetFromJsonAsync<ProductBody>($"/api/v1/products/{id}");
        Assert.Equal(new ProductBody(id, name, null, 12.50m, 8, IsActive: true), product);
    }

    [Fact]
    public async Task A_deleted_product_is_gone()
    {
        using var client = CatalogClient();
        var id = await CreateProductAsync(client);

        var deleted = await client.DeleteAsync($"/api/v1/products/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        var read = await client.GetAsync($"/api/v1/products/{id}");
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        var problem = await read.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("product.not_found", problem.GetProperty("code").GetString());
    }

    private HttpClient CatalogClient() => factory.CreateClientWithPermissions(
        CatalogPermissions.ProductsCreate, CatalogPermissions.ProductsRead,
        CatalogPermissions.ProductsUpdate, CatalogPermissions.ProductsDelete);

    private static async Task<Guid> CreateProductAsync(HttpClient client)
    {
        var created = await client.PostAsJsonAsync("/api/v1/products",
            new { name = UniqueName(), price = 10m, stock = 3 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    // The tests share one database: a unique name finds this test's product in the list.
    private static string UniqueName() => $"Widget {Guid.NewGuid():N}";

    private sealed record ProductBody(Guid Id, string Name, string? Description, decimal Price, int Stock, bool IsActive);

    private sealed record ProductPage(IReadOnlyList<ProductBody> Items, long TotalCount);
}
