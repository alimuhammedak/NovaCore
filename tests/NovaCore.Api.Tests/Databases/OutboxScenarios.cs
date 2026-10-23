using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Domain;
using Xunit;

namespace NovaCore.Api.Tests.Databases;

/// <summary>
/// The transactional outbox on a real database, the same on every provider, with the Api host's own dispatcher on the
/// real clock: a payment's <c>PaymentCreated</c> is written with the payment, delivered to the Catalog module, whose
/// answer is written with the stock change and delivered back; the retention sweep deletes, in SQL on the
/// <c>(ProcessedOnUtc, Id)</c> index, a processed message older than 7 days with its records and keeps a younger one.
/// </summary>
public abstract class OutboxScenarios<TFactory>(TFactory factory) : IClassFixture<TFactory>, IAsyncLifetime
    where TFactory : DatabaseApiFactory
{
    // OutboxDispatcher.ProcessedRetention (internal).
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    protected TFactory Factory { get; } = factory;

    public async ValueTask InitializeAsync() => await Factory.ResetAsync();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task A_payment_takes_its_stock_and_completes_through_the_outbox()
    {
        var productId = await Factory.AddProductAsync(stock: 5);
        using var client = Factory.CreateClientWithPermissions(PaymentsPermissions.Create);

        var response = await client.PostAsJsonAsync("/api/v1/payments", new { productId, quantity = 2 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var paymentId = (await response.Content.ReadFromJsonAsync<CreatedPayment>())!.PaymentId;

        // One pass hands PaymentCreated to the Catalog module, a later one hands its answer to the Payments module.
        await UntilAsync(async () => await Factory.PaymentStatusAsync(paymentId) == PaymentStatus.Completed);

        Assert.Equal(PaymentStatus.Completed, await Factory.PaymentStatusAsync(paymentId));
        Assert.Equal(3, await Factory.StockAsync(productId));
    }

    [Fact]
    public async Task The_dispatcher_deletes_the_processed_messages_past_the_retention_with_their_records()
    {
        var now = TimeProvider.System.GetUtcNow();
        var expired = await AddProcessedMessageAsync(now - Retention - TimeSpan.FromHours(1));
        var kept = await AddProcessedMessageAsync(now - Retention + TimeSpan.FromHours(1));

        await UntilAsync(async () => await RecordsAsync(expired) == 0);

        Assert.Equal(0, await RecordsAsync(expired));
        Assert.Equal(0, await MessagesAsync(expired));
        Assert.Equal(1, await RecordsAsync(kept));
        Assert.Equal(1, await MessagesAsync(kept));
    }

    // The dispatcher's passes run every few seconds on the real clock.
    private static async Task UntilAsync(Func<Task<bool>> condition)
    {
        var deadline = TimeProvider.System.GetUtcNow() + TimeSpan.FromSeconds(60);
        while (!await condition() && TimeProvider.System.GetUtcNow() < deadline)
            await Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);
    }

    // A message processed at processedOnUtc, with one handler's record, as an earlier pass would have left it.
    private async Task<Guid> AddProcessedMessageAsync(DateTimeOffset processedOnUtc)
    {
        var eventId = Guid.NewGuid();
        await using var scope = Factory.Services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var sql = new PortableSql(catalog);

        var message = $"INSERT INTO {sql.Table("outbox_messages")} ({sql.Column("EventId")}, {sql.Column("Type")}, " +
                      $"{sql.Column("Content")}, {sql.Column("OccurredOnUtc")}, {sql.Column("ProcessedOnUtc")}, " +
                      $"{sql.Column("Attempts")}) VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {{3}}, 0)";
        await catalog.Database.ExecuteSqlRawAsync(message,
            [eventId, "System.String, System.Private.CoreLib", "\"text\"", processedOnUtc],
            TestContext.Current.CancellationToken);

        var record = $"INSERT INTO {sql.Table("outbox_consumers")} ({sql.Column("MessageId")}, {sql.Column("Handler")}, " +
                     $"{sql.Column("ProcessedOnUtc")}) SELECT {sql.Column("Id")}, {{1}}, {{2}} " +
                     $"FROM {sql.Table("outbox_messages")} WHERE {sql.Column("EventId")} = {{0}}";
        await catalog.Database.ExecuteSqlRawAsync(record,
            [eventId, "Sample.Handler", processedOnUtc], TestContext.Current.CancellationToken);
        return eventId;
    }

    // The handler records of the message with eventId (none once it is deleted).
    private Task<int> RecordsAsync(Guid eventId) => CountAsync(sql =>
        $"FROM {sql.Table("outbox_consumers")} c JOIN {sql.Table("outbox_messages")} m " +
        $"ON m.{sql.Column("Id")} = c.{sql.Column("MessageId")} WHERE m.{sql.Column("EventId")} = {{0}}", eventId);

    private Task<int> MessagesAsync(Guid eventId) => CountAsync(sql =>
        $"FROM {sql.Table("outbox_messages")} WHERE {sql.Column("EventId")} = {{0}}", eventId);

    // COUNT(*) is an int on SQL Server and a bigint on PostgreSQL; INTEGER is both servers' name for an int.
    private async Task<int> CountAsync(Func<PortableSql, string> from, Guid eventId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var sql = new PortableSql(catalog);
        var query = $"SELECT CAST(COUNT(*) AS INTEGER) AS {sql.Column("Value")} {from(sql)}";
        return await catalog.Database.SqlQueryRaw<int>(query, eventId).SingleAsync(TestContext.Current.CancellationToken);
    }

    private sealed record CreatedPayment(Guid PaymentId);
}
