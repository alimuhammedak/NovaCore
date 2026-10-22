using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Modules.Catalog.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.SqlServer;

/// <summary>
/// The outbox dispatcher's retention sweep on SQL Server, where its <c>ProcessedOnUtc &lt; cutoff</c> runs as SQL on the
/// <c>(ProcessedOnUtc, Id)</c> index: the Api host's own dispatcher deletes a processed message older than the 7-day
/// retention with its records and keeps a younger one.
/// </summary>
public sealed class OutboxRetentionTests(SqlServerApiFactory factory) : IClassFixture<SqlServerApiFactory>, IAsyncLifetime
{
    // OutboxDispatcher.ProcessedRetention (internal).
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    public async ValueTask InitializeAsync() => await factory.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task The_dispatcher_deletes_the_processed_messages_past_the_retention_with_their_records()
    {
        var now = TimeProvider.System.GetUtcNow();
        var expired = await AddProcessedMessageAsync(now - Retention - TimeSpan.FromHours(1));
        var kept = await AddProcessedMessageAsync(now - Retention + TimeSpan.FromHours(1));

        // A pass runs every few seconds on the real clock.
        var deadline = now + TimeSpan.FromSeconds(30);
        while (await RecordsAsync(expired) > 0 && TimeProvider.System.GetUtcNow() < deadline)
            await Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);

        Assert.Equal(0, await RecordsAsync(expired));
        Assert.Equal(0, await MessagesAsync(expired));
        Assert.Equal(1, await RecordsAsync(kept));
        Assert.Equal(1, await MessagesAsync(kept));
    }

    // A message processed at processedOnUtc, with one handler's record, as an earlier pass would have left it.
    private async Task<Guid> AddProcessedMessageAsync(DateTimeOffset processedOnUtc)
    {
        var eventId = Guid.NewGuid();
        await using var scope = factory.Services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await catalog.Database.ExecuteSqlRawAsync("""
            INSERT INTO [catalog].[outbox_messages] (EventId, Type, Content, OccurredOnUtc, ProcessedOnUtc, Attempts)
            VALUES ({0}, N'System.String, System.Private.CoreLib', N'"text"', {1}, {1}, 0);
            INSERT INTO [catalog].[outbox_consumers] (MessageId, Handler, ProcessedOnUtc)
            SELECT Id, N'Sample.Handler', {1} FROM [catalog].[outbox_messages] WHERE EventId = {0};
            """, [eventId, processedOnUtc], TestContext.Current.CancellationToken);
        return eventId;
    }

    // The handler records of the message with eventId (none once it is deleted).
    private Task<int> RecordsAsync(Guid eventId) => CountAsync("""
        SELECT COUNT(*) AS Value FROM [catalog].[outbox_consumers] c
        JOIN [catalog].[outbox_messages] m ON m.Id = c.MessageId WHERE m.EventId = {0}
        """, eventId);

    private Task<int> MessagesAsync(Guid eventId) =>
        CountAsync("SELECT COUNT(*) AS Value FROM [catalog].[outbox_messages] WHERE EventId = {0}", eventId);

    private async Task<int> CountAsync(string sql, Guid eventId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database
            .SqlQueryRaw<int>(sql, eventId)
            .SingleAsync(TestContext.Current.CancellationToken);
    }
}
