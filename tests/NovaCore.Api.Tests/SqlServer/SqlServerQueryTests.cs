using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Identity;
using NovaCore.Identity.OpenIddict;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Domain;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Domain;
using NovaCore.Modules.Payments.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.SqlServer;

/// <summary>
/// What SQLite can't show, on a real SQL Server: the payment list's <c>DateTimeOffset</c> order (SQLite can't translate
/// it, R-068), the modules' schemas, the collation behind <c>EF.Functions.Like</c> and <c>decimal(18,2)</c>.
/// </summary>
public sealed class SqlServerQueryTests(SqlServerApiFactory factory) : IClassFixture<SqlServerApiFactory>, IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

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

    [Fact]
    public async Task Each_context_keeps_its_tables_in_its_own_schema()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        DbContext[] contexts =
        [
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>(),
            scope.ServiceProvider.GetRequiredService<PaymentsDbContext>(),
            scope.ServiceProvider.GetRequiredService<IdentityServerDbContext>()
        ];

        var tables = await contexts[0].Database
            .SqlQueryRaw<TableName>(
                "SELECT TABLE_SCHEMA AS [Schema], TABLE_NAME AS [Name] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
            .ToListAsync();

        foreach (var context in contexts)
        {
            var schema = context.Model.GetDefaultSchema()!;
            var modelTables = context.Model.GetEntityTypes()
                .Where(entity => entity.GetTableName() is not null)
                .Select(entity => new TableName(entity.GetSchema()!, entity.GetTableName()!))
                .Append(new TableName(schema, "__EFMigrationsHistory"))
                .Distinct();

            Assert.Equal(InOrder(modelTables), InOrder(tables.Where(table => table.Schema == schema)));
        }

        // Nothing outside the three schemas (no dbo).
        Assert.Equal(
            contexts.Select(context => context.Model.GetDefaultSchema()).Order(StringComparer.Ordinal),
            tables.Select(table => table.Schema).Distinct().Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Product_search_ignores_case_but_not_accents_on_the_servers_default_collation()
    {
        await AddProductsAsync("Blue Widget", "BLUE gadget", "Red widget", "Café crème");
        using var client = factory.CreateClientWithPermissions(CatalogPermissions.ProductsRead);

        Assert.Equal(["BLUE gadget", "Blue Widget"], await SearchAsync(client, "blue"));
        Assert.Equal(["Café crème"], await SearchAsync(client, "CAFÉ"));
        Assert.Empty(await SearchAsync(client, "cafe"));
    }

    // decimal(18,2) holds these as they are; validation refuses what it would round or can't hold (ValidationTests).
    [Theory]
    [InlineData("0.01")]
    [InlineData("12345678.99")]
    [InlineData("9999999999999999.99")]
    public async Task A_price_the_column_holds_reads_back_unchanged(string value)
    {
        var price = decimal.Parse(value, CultureInfo.InvariantCulture);
        using var client = factory.CreateClientWithPermissions(
            CatalogPermissions.ProductsCreate, CatalogPermissions.ProductsRead);

        var created = await client.PostAsJsonAsync("/api/v1/products", new { name = "Widget", price, stock = 1 });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var product = await client.GetFromJsonAsync<JsonElement>(created.Headers.Location);
        Assert.Equal(price, product.GetProperty("price").GetDecimal());
    }

    private HttpClient ClientFor(string subject, params string[] permissions)
    {
        factory.GrantPermissions(subject, permissions);
        return factory.CreateClientWithClaims(new Claim(NovaClaims.Subject, subject));
    }

    // Kept out of the outbox: the dispatcher would settle the payment while the test reads it.
    private async Task<Guid> AddPaymentAsync(string owner)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var payment = new Payment(owner, Guid.NewGuid(), "Widget", 1, 10m);
        payment.ClearDomainEvents();
        payments.Payments.Add(payment);
        await payments.SaveChangesAsync();
        return payment.Id;
    }

    private async Task AddProductsAsync(params string[] names)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        catalog.Products.AddRange(names.Select(name => new Product(name, null, 10m, 1)));
        await catalog.SaveChangesAsync();
    }

    private static async Task<IEnumerable<string>> SearchAsync(HttpClient client, string search)
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/v1/products?search={Uri.EscapeDataString(search)}");
        return page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()!).ToList();
    }

    private sealed record Page(IReadOnlyList<PaymentItem> Items, long TotalCount);

    private sealed record PaymentItem(Guid Id);

    private static List<TableName> InOrder(IEnumerable<TableName> tables) =>
        tables.OrderBy(table => table.Schema, StringComparer.Ordinal).ThenBy(table => table.Name, StringComparer.Ordinal).ToList();

    private sealed record TableName(string Schema, string Name);
}
