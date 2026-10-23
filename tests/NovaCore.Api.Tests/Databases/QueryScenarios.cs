using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Identity;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Domain;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Domain;
using NovaCore.Modules.Payments.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.Databases;

/// <summary>
/// What SQLite can't show, on a real database, the same on every provider: the payment list's <c>DateTimeOffset</c>
/// order (SQLite can't translate it, R-068) and a price the <c>decimal(18,2)</c> column holds. Text comparison follows
/// the server's collation, which differs between providers: each provider's class pins its own.
/// </summary>
public abstract class QueryScenarios<TFactory>(TFactory factory) : IClassFixture<TFactory>, IAsyncLifetime
    where TFactory : DatabaseApiFactory
{
    protected TFactory Factory { get; } = factory;

    public async ValueTask InitializeAsync() => await Factory.ResetAsync();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task The_payment_list_shows_the_callers_own_payments_newest_first_a_page_at_a_time()
    {
        var owner = Guid.NewGuid().ToString();
        var first = await AddPaymentAsync(owner);
        var second = await AddPaymentAsync(owner);
        var third = await AddPaymentAsync(owner);
        await AddPaymentAsync(Guid.NewGuid().ToString());
        using var client = ClientFor(owner, PaymentsPermissions.Read);

        var firstPage = await client.GetFromJsonAsync<Page>("/api/v1/payments?page=1&pageSize=2");
        var secondPage = await client.GetFromJsonAsync<Page>("/api/v1/payments?page=2&pageSize=2");

        Assert.Equal([third, second], firstPage!.Items.Select(payment => payment.Id));
        Assert.Equal([first], secondPage!.Items.Select(payment => payment.Id));
        Assert.Equal(3, firstPage.TotalCount);
    }

    [Fact]
    public async Task The_payment_list_shows_everyones_payments_with_read_all()
    {
        var mine = Guid.NewGuid().ToString();
        var older = await AddPaymentAsync(Guid.NewGuid().ToString());
        var newer = await AddPaymentAsync(mine);
        using var client = ClientFor(mine, PaymentsPermissions.Read, PaymentsPermissions.ReadAll);

        var page = await client.GetFromJsonAsync<Page>("/api/v1/payments");

        Assert.Equal([newer, older], page!.Items.Select(payment => payment.Id));
    }

    // The column holds these as they are; validation refuses what it would round or can't hold (ValidationTests).
    [Theory]
    [InlineData("0.01")]
    [InlineData("12345678.99")]
    [InlineData("9999999999999999.99")]
    public async Task A_price_the_column_holds_reads_back_unchanged(string value)
    {
        var price = decimal.Parse(value, CultureInfo.InvariantCulture);
        using var client = Factory.CreateClientWithPermissions(
            CatalogPermissions.ProductsCreate, CatalogPermissions.ProductsRead);

        var created = await client.PostAsJsonAsync("/api/v1/products", new { name = "Widget", price, stock = 1 });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var product = await client.GetFromJsonAsync<JsonElement>(created.Headers.Location);
        Assert.Equal(price, product.GetProperty("price").GetDecimal());
    }

    protected async Task AddProductsAsync(params string[] names)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        catalog.Products.AddRange(names.Select(name => new Product(name, null, 10m, 1)));
        await catalog.SaveChangesAsync();
    }

    protected static async Task<IEnumerable<string>> SearchAsync(HttpClient client, string search)
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/v1/products?search={Uri.EscapeDataString(search)}");
        return page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()!).ToList();
    }

    private HttpClient ClientFor(string subject, params string[] permissions)
    {
        Factory.GrantPermissions(subject, permissions);
        return Factory.CreateClientWithClaims(new Claim(NovaClaims.Subject, subject));
    }

    // Kept out of the outbox: the dispatcher would settle the payment while the test reads it.
    private async Task<Guid> AddPaymentAsync(string owner)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var payment = new Payment(owner, Guid.NewGuid(), "Widget", 1, 10m);
        payment.ClearDomainEvents();
        payments.Payments.Add(payment);
        await payments.SaveChangesAsync();
        return payment.Id;
    }

    private sealed record Page(IReadOnlyList<PaymentItem> Items, long TotalCount);

    private sealed record PaymentItem(Guid Id);
}
