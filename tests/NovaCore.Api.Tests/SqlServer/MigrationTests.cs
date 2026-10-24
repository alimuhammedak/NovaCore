using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Api.Catalog;
using NovaCore.Api.Identity;
using NovaCore.Api.Payments;
using NovaCore.Api.Tests.Databases;
using NovaCore.Identity.OpenIddict;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.SqlServer;

/// <summary>
/// The migrations on SQL Server: the startup migrations every provider runs (<see cref="MigrationScenarios{TFactory}"/>),
/// what only the host's SQL Server migrations have, their data steps (raw SQL, in <c>EXEC</c>, carrying existing rows
/// over both ways), and the design-time factories `dotnet ef` builds them with. CI's <c>has-pending-model-changes</c>
/// only compares the model with them.
/// </summary>
public sealed class MigrationTests(SqlServerApiFactory factory) : MigrationScenarios<SqlServerApiFactory>(factory)
{
    private const string Handler = "Sample.Handler";

    protected override IReadOnlyCollection<string> SystemSchemas { get; } = [];

    [Fact]
    public async Task The_design_time_factories_build_the_contexts_the_host_runs()
    {
        // `dotnet ef` (migrations, scripts, bundles) builds the contexts with the host's design-time factories, the host
        // with AddModuleDatabase and AddModuleDbContext: the same provider, migrations and history table, or a bundle
        // would apply the migrations somewhere the host doesn't look (EF Core: "Design-time DbContext Creation").
        Factory.Database.SkipIfUnavailable();
        await using var scope = Factory.Services.CreateAsyncScope();

        foreach (var (runtime, designTime) in new (DbContext, DbContext)[]
                 {
                     (scope.ServiceProvider.GetRequiredService<CatalogDbContext>(), new CatalogDbContextFactory().CreateDbContext([])),
                     (scope.ServiceProvider.GetRequiredService<PaymentsDbContext>(), new PaymentsDbContextFactory().CreateDbContext([])),
                     (scope.ServiceProvider.GetRequiredService<IdentityServerDbContext>(), new IdentityServerDbContextFactory().CreateDbContext([]))
                 })
        {
            await using var _ = designTime;
            Assert.Equal(runtime.Database.ProviderName, designTime.Database.ProviderName);
            Assert.Equal(runtime.GetService<IMigrationsAssembly>().Assembly, designTime.GetService<IMigrationsAssembly>().Assembly);
            Assert.Equal(runtime.Database.GetMigrations(), designTime.Database.GetMigrations(), StringComparer.Ordinal);
            Assert.Equal(
                runtime.GetService<IHistoryRepository>().GetCreateScript(),
                designTime.GetService<IHistoryRepository>().GetCreateScript());
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

    // A module context on its own, as `dotnet ef` builds it (the host's design-time factory), on the given database.
    private static DbContext ModuleContext(string schema, string connectionString)
    {
        DbContext context = schema switch
        {
            CatalogDbContext.Schema => new CatalogDbContextFactory().CreateDbContext([]),
            PaymentsDbContext.Schema => new PaymentsDbContextFactory().CreateDbContext([]),
            _ => throw new ArgumentOutOfRangeException(nameof(schema), schema, "Not a module schema.")
        };
        context.Database.SetConnectionString(connectionString);
        return context;
    }

    private static string InSchema(string schema, string sql) => sql.Replace("{schema}", schema, StringComparison.Ordinal);

    private sealed record ConsumerRecord(long MessageId, string Handler);
}
