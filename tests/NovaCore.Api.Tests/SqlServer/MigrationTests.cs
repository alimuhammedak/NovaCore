using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Identity.OpenIddict;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.SqlServer;

/// <summary>
/// The migrations on a real SQL Server. The SQLite tests never run them (their schema comes from <c>EnsureCreated</c>) and
/// CI's <c>has-pending-model-changes</c> only compares the model with them, so this is where their SQL runs: every
/// migration applies to an empty database through the host's own startup migration, and the data steps carry existing
/// rows over (their raw SQL, in <c>EXEC</c>, both ways).
/// </summary>
public class MigrationTests(SqlServerApiFactory factory) : IClassFixture<SqlServerApiFactory>
{
    private const string Handler = "Sample.Handler";

    [Fact]
    public async Task The_host_applies_every_migration_of_every_context_when_it_starts()
    {
        factory.Database.SkipIfUnavailable();
        await using var scope = factory.Services.CreateAsyncScope();

        foreach (var context in new DbContext[]
                 {
                     scope.ServiceProvider.GetRequiredService<CatalogDbContext>(),
                     scope.ServiceProvider.GetRequiredService<PaymentsDbContext>(),
                     scope.ServiceProvider.GetRequiredService<IdentityServerDbContext>()
                 })
        {
            Assert.NotEmpty(context.Database.GetMigrations());
            Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        }
    }

    [Theory]
    [InlineData(CatalogDbContext.Schema, "20260928105234_StockConcurrencyToken")]
    [InlineData(PaymentsDbContext.Schema, "20260927214259_OutboxDispatch")]
    public async Task OutboxConsumersByMessage_keys_the_records_by_their_message_both_ways(string schema, string previous)
    {
        var database = await TestSqlServer.CreateDatabaseAsync();
        database.SkipIfUnavailable();
        await using var context = ModuleContext(schema, database.ConnectionString);
        var migrator = context.GetService<IMigrator>();

        // Before the migration: records keyed by the event id, one of them for a message that is gone.
        await migrator.MigrateAsync(previous);
        var eventId = Guid.NewGuid();
        await context.Database.ExecuteSqlRawAsync(InSchema(schema, """
            INSERT INTO [{schema}].[outbox_messages] (EventId, Type, Content, OccurredOnUtc, Attempts, ProcessedOnUtc)
            VALUES ({0}, N'Sample.Event, Sample', N'null', SYSDATETIMEOFFSET(), 0, SYSDATETIMEOFFSET());
            INSERT INTO [{schema}].[outbox_consumers] (EventId, Handler, ProcessedOnUtc)
            VALUES ({0}, {1}, SYSDATETIMEOFFSET()), ({2}, {1}, SYSDATETIMEOFFSET());
            """), eventId, Handler, Guid.NewGuid());
        var messageId = await context.Database
            .SqlQueryRaw<long>(InSchema(schema, "SELECT Id AS Value FROM [{schema}].[outbox_messages]"))
            .SingleAsync();

        await migrator.MigrateAsync();
        Assert.Equal(
            [new ConsumerRecord(messageId, Handler)],
            await context.Database
                .SqlQueryRaw<ConsumerRecord>(InSchema(schema, "SELECT MessageId, Handler FROM [{schema}].[outbox_consumers]"))
                .ToListAsync());

        // Down carries the record back to the event id.
        await migrator.MigrateAsync(previous);
        Assert.Equal(
            [eventId],
            await context.Database
                .SqlQueryRaw<Guid>(InSchema(schema, "SELECT EventId AS Value FROM [{schema}].[outbox_consumers]"))
                .ToListAsync());

        // Up again: deleting the message deletes its records (the foreign key cascades).
        await migrator.MigrateAsync();
        await context.Database.ExecuteSqlRawAsync(InSchema(schema, "DELETE FROM [{schema}].[outbox_messages]"));
        Assert.Equal(0, await context.Database
            .SqlQueryRaw<int>(InSchema(schema, "SELECT COUNT(*) AS Value FROM [{schema}].[outbox_consumers]"))
            .SingleAsync());
    }

    [Fact]
    public async Task PaymentOwner_gives_the_payments_before_it_an_empty_owner()
    {
        var database = await TestSqlServer.CreateDatabaseAsync();
        database.SkipIfUnavailable();
        await using var context = ModuleContext(PaymentsDbContext.Schema, database.ConnectionString);
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync("20261020000721_OutboxConsumersByMessage");
        var paymentId = Guid.NewGuid();
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO [payments].[payments] (Id, ProductId, ProductName, Quantity, UnitPrice, Amount, Status, CreatedAtUtc, CreatedBy)
            VALUES ({0}, {1}, N'Widget', 1, 10, 10, N'Completed', SYSDATETIMEOFFSET(), N'someone');
            """, paymentId, Guid.NewGuid());

        await migrator.MigrateAsync();

        Assert.Equal(string.Empty, await context.Database
            .SqlQueryRaw<string>("SELECT OwnerId AS Value FROM [payments].[payments] WHERE Id = {0}", paymentId)
            .SingleAsync());
    }

    // A module context on its own, configured the way the module registers it: its schema keeps its migration history.
    private static DbContext ModuleContext(string schema, string connectionString) => schema switch
    {
        CatalogDbContext.Schema => new CatalogDbContext(Options<CatalogDbContext>(connectionString, schema)),
        PaymentsDbContext.Schema => new PaymentsDbContext(Options<PaymentsDbContext>(connectionString, schema)),
        _ => throw new ArgumentOutOfRangeException(nameof(schema), schema, "Not a module schema.")
    };

    private static DbContextOptions<TContext> Options<TContext>(string connectionString, string schema)
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", schema))
            .Options;

    private static string InSchema(string schema, string sql) => sql.Replace("{schema}", schema, StringComparison.Ordinal);

    private sealed record ConsumerRecord(long MessageId, string Handler);
}
