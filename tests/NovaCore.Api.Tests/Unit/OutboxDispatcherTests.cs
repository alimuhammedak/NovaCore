using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NovaCore.Abstractions.Entities;
using NovaCore.Abstractions.Messaging;
using NovaCore.EntityFrameworkCore.Messaging;
using NovaCore.EntityFrameworkCore.Persistence;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// PLAN F4a.2: the outbox dispatcher hands every saved event to each of its handlers once. It records every handler
/// that finished (<c>outbox_consumers</c>, keyed by the message), retries the others on later passes, gives up after ten
/// attempts and opens no transaction of its own, so it works under a retrying execution strategy. PLAN F4c.7: a processed
/// message is deleted, with its handlers' records, once it is older than the retention. The clock is a
/// <see cref="FakeTimeProvider"/>: the dispatcher runs a pass when the host starts and whenever the clock moves past the
/// polling interval.
/// </summary>
public sealed class OutboxDispatcherTests : IAsyncDisposable
{
    private static readonly TimeSpan NextPass = TimeSpan.FromMinutes(1);

    // OutboxDispatcher.ProcessedRetention, which is internal to NovaCore.EntityFrameworkCore.
    private static readonly TimeSpan Retention = TimeSpan.FromDays(7);

    // A named in-memory database that every connection with this name shares; this open connection keeps it alive (no
    // pooling, so nothing else does). The dispatcher, the test and the handlers use their own connections: a
    // SqliteConnection isn't thread-safe (R-069).
    private readonly SqliteConnection _database =
        new($"Data Source=outbox-dispatcher-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False");

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly HandlerLog _log = new();

    public OutboxDispatcherTests() => _database.Open();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task A_message_is_handled_once()
    {
        void Handlers(IServiceCollection services) =>
            services.AddScoped<IDomainEventHandler<OrderPlaced>, RecordingHandler>();

        Guid first, second;
        using (var host = await CreateHostAsync(Handlers))
        {
            first = await SaveOrderAsync(host);
            await host.StartAsync(TestContext.Current.CancellationToken);
            await WaitUntilAsync(async () => (await MessageAsync(first)).ProcessedOnUtc is not null);

            // Each later pass shows up as the next saved event being handled.
            second = await SaveOrderAsync(host);
            _clock.Advance(NextPass);
            await WaitUntilAsync(async () => (await MessageAsync(second)).ProcessedOnUtc is not null);
            await host.StopAsync(TestContext.Current.CancellationToken);
        }

        // A host started over the same database does not hand the handled events over again.
        using var restarted = await CreateHostAsync(Handlers);
        var third = await SaveOrderAsync(restarted);
        await restarted.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => (await MessageAsync(third)).ProcessedOnUtc is not null);
        await restarted.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            [(nameof(RecordingHandler), first), (nameof(RecordingHandler), second), (nameof(RecordingHandler), third)],
            _log.Calls);
    }

    [Fact]
    public async Task A_failed_handler_is_retried_without_running_the_handlers_that_succeeded_again()
    {
        using var host = await CreateHostAsync(services => services
            .AddScoped<IDomainEventHandler<OrderPlaced>, RecordingHandler>()
            .AddScoped<IDomainEventHandler<OrderPlaced>, FailsOnceHandler>());
        var placed = await SaveOrderAsync(host);

        await host.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => (await MessageAsync(placed)).Attempts == 1);
        _clock.Advance(NextPass);
        await WaitUntilAsync(async () => (await MessageAsync(placed)).ProcessedOnUtc is not null);
        await host.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            [(nameof(RecordingHandler), placed), (nameof(FailsOnceHandler), placed), (nameof(FailsOnceHandler), placed)],
            _log.Calls);
        Assert.Equal(
            [typeof(FailsOnceHandler).FullName!, typeof(RecordingHandler).FullName!],
            (await ConsumersAsync(placed)).Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    [Fact]
    public async Task A_message_that_keeps_failing_is_left_for_inspection_after_ten_attempts()
    {
        using var host = await CreateHostAsync(services =>
            services.AddScoped<IDomainEventHandler<OrderPlaced>, AlwaysFailsHandler>());
        var poisoned = await SaveOrderAsync(host);

        // Keep the passes coming (one advance can make two: ticks during a pass are kept) until the attempts run out.
        await host.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () =>
        {
            _clock.Advance(NextPass);
            return (await MessageAsync(poisoned)).Attempts == 10;
        });

        // Later passes, marked by the next event's first attempt, leave the poisoned message alone.
        var next = await SaveOrderAsync(host);
        await WaitUntilAsync(async () =>
        {
            _clock.Advance(NextPass);
            return (await MessageAsync(next)).Attempts >= 1;
        });
        await host.StopAsync(TestContext.Current.CancellationToken);

        var message = await MessageAsync(poisoned);
        Assert.Equal(10, message.Attempts);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Contains(AlwaysFailsHandler.Failure, message.Error, StringComparison.Ordinal);
        Assert.Equal(10, _log.Calls.Count(call => call.EventId == poisoned));
    }

    // The rows below are aged by their timestamps, not by advancing the clock: FakeTimeProvider fires a periodic timer
    // once per period it advances over, and the dispatcher's 5-second timer would fire 120,000 times in 7 days.
    [Fact]
    public async Task A_processed_message_is_deleted_with_its_records_once_the_retention_has_passed()
    {
        using var host = await CreateHostAsync(services =>
            services.AddScoped<IDomainEventHandler<OrderPlaced>, RecordingHandler>());
        var now = _clock.GetUtcNow();
        var expired = await AddProcessedMessageAsync(now - Retention - TimeSpan.FromMinutes(1), "ExpiredHandler");
        var kept = await AddProcessedMessageAsync(now - Retention + TimeSpan.FromMinutes(1), "KeptHandler");
        var delivered = await SaveOrderAsync(host);

        // The pass at startup delivers the new message, then sweeps.
        await host.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => !await MessageExistsAsync(expired));
        await host.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["KeptHandler"], await ConsumersAsync(kept));
        Assert.Equal([typeof(RecordingHandler).FullName!], await ConsumersAsync(delivered));
        Assert.Equal(2, await ConsumerCountAsync());   // the expired message's record went with it
    }

    [Fact]
    public async Task A_message_processed_late_does_not_hold_back_the_expired_ones_after_it()
    {
        using var host = await CreateHostAsync(services =>
            services.AddScoped<IDomainEventHandler<OrderPlaced>, RecordingHandler>());
        var now = _clock.GetUtcNow();
        // The lowest Id, processed just now (as after retries). The sweep goes by processing time, so it neither stops at this
        // message nor keeps the 50 expired ones after it, one full batch and an empty one.
        var late = await AddProcessedMessageAsync(now - TimeSpan.FromMinutes(1), "LateHandler");
        for (var i = 0; i < 50; i++)
            await AddProcessedMessageAsync(now - Retention - TimeSpan.FromDays(1), "ExpiredHandler");
        await SaveOrderAsync(host);

        // One pass, the one at startup, takes all 50.
        await host.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => await ConsumerCountAsync() == 2);
        await host.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["LateHandler"], await ConsumersAsync(late));
    }

    [Fact]
    public async Task A_message_left_for_inspection_is_kept_whatever_its_age()
    {
        using var host = await CreateHostAsync(services =>
            services.AddScoped<IDomainEventHandler<OrderPlaced>, RecordingHandler>());
        var poisoned = Guid.CreateVersion7();
        await using (var context = CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO outbox_messages (EventId, Type, Content, OccurredOnUtc, Attempts, Error) VALUES ({0}, {1}, {2}, {3}, 10, 'failed')",
                [poisoned, "System.String, System.Private.CoreLib", "\"text\"", _clock.GetUtcNow() - TimeSpan.FromDays(30)],
                TestContext.Current.CancellationToken);
        }
        var delivered = await SaveOrderAsync(host);

        await host.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => (await MessageAsync(delivered)).ProcessedOnUtc is not null);
        await host.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(10, (await MessageAsync(poisoned)).Attempts);
    }

    [Fact]
    public async Task A_handler_registered_twice_runs_once()
    {
        using var host = await CreateHostAsync(services => services
            .AddScoped<IDomainEventHandler<OrderPlaced>, RecordingHandler>()
            .AddScoped<IDomainEventHandler<OrderPlaced>, RecordingHandler>());
        var placed = await SaveOrderAsync(host);

        await host.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => (await MessageAsync(placed)).ProcessedOnUtc is not null);
        await host.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal([(nameof(RecordingHandler), placed)], _log.Calls);
        Assert.Equal(0, (await MessageAsync(placed)).Attempts);
    }

    [Fact]
    public async Task A_handler_whose_name_does_not_fit_its_record_is_not_run()
    {
        // Without a record, every retry would run it again.
        var handlerType = typeof(GenericHandler<AMarkerWhoseNameMakesTheClosedGenericHandlerNameLongerThanTheTwoHundredFiftySixCharactersItsRecordKeeps>);
        Assert.True(handlerType.FullName!.Length > 256);
        using var host = await CreateHostAsync(services =>
            services.AddScoped(typeof(IDomainEventHandler<OrderPlaced>), handlerType));
        var placed = await SaveOrderAsync(host);

        await host.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => (await MessageAsync(placed)).Attempts == 1);
        await host.StopAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_log.Calls);
        Assert.Contains("256", (await MessageAsync(placed)).Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stopping_the_host_during_a_delivery_does_not_fault_the_dispatcher()
    {
        using var host = await CreateHostAsync(services =>
            services.AddScoped<IDomainEventHandler<OrderPlaced>, FailsWhenStoppedHandler>());
        await SaveOrderAsync(host);

        await host.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => Task.FromResult(_log.Calls.Count == 1));
        await host.StopAsync(TestContext.Current.CancellationToken);

        var dispatcher = host.Services.GetServices<IHostedService>().OfType<BackgroundService>().Single();
        Assert.False(dispatcher.ExecuteTask!.IsFaulted, dispatcher.ExecuteTask.Exception?.ToString());
    }

    [Fact]
    public async Task Events_without_handlers_are_marked_as_processed()
    {
        using var host = await CreateHostAsync(_ => { });
        var placed = await SaveOrderAsync(host);

        await host.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => (await MessageAsync(placed)).ProcessedOnUtc is not null);
        await host.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, (await MessageAsync(placed)).Attempts);
        Assert.Empty(await ConsumersAsync(placed));
    }

    [Fact]
    public async Task A_message_whose_type_is_not_a_domain_event_is_a_failed_attempt()
    {
        using var host = await CreateHostAsync(_ => { });
        var eventId = Guid.CreateVersion7();
        await using (var context = CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO outbox_messages (EventId, Type, Content, OccurredOnUtc, Attempts) VALUES ({0}, {1}, {2}, {3}, 0)",
                [eventId, "System.String, System.Private.CoreLib", "\"text\"", _clock.GetUtcNow()],
                TestContext.Current.CancellationToken);
        }

        await host.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => (await MessageAsync(eventId)).Attempts == 1);
        await host.StopAsync(TestContext.Current.CancellationToken);

        Assert.Contains("System.String", (await MessageAsync(eventId)).Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dispatch_works_under_a_retrying_execution_strategy()
    {
        // Recording a finished handler fails once with an error the strategy retries: the handler must not run again.
        var failOnce = new FailFirstConsumerInsert();
        using var host = await CreateHostAsync(
            services => services.AddScoped<IDomainEventHandler<OrderPlaced>, RecordingHandler>(),
            sqlite => sqlite.ExecutionStrategy(dependencies => new RetryingStrategy(dependencies)),
            failOnce);
        var placed = await SaveOrderAsync(host);

        await host.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(async () => (await MessageAsync(placed)).ProcessedOnUtc is not null);
        await host.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(failOnce.Failed);
        Assert.Equal([(nameof(RecordingHandler), placed)], _log.Calls);
        Assert.Equal([typeof(RecordingHandler).FullName!], await ConsumersAsync(placed));
    }

    private async Task<IHost> CreateHostAsync(
        Action<IServiceCollection> registerHandlers,
        Action<SqliteDbContextOptionsBuilder>? sqlite = null,
        params IInterceptor[] interceptors)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<TimeProvider>(_clock);
        builder.Services.AddSingleton(_log);
        builder.AddCorePersistence();
        builder.Services.AddModuleDbContext<ShopDbContext>((_, options) => options
            .UseSqlite(_database.ConnectionString, sqlite)
            .ReplaceService<IModelCustomizer, UtcTicksModelCustomizer>()
            .AddInterceptors(interceptors));
        registerHandlers(builder.Services);
        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateScopes = true }));

        var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database
            .EnsureCreatedAsync(TestContext.Current.CancellationToken);

        return host;
    }

    private static async Task<Guid> SaveOrderAsync(IHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var order = new Order(Guid.CreateVersion7());
        var eventId = order.DomainEvents.Single().EventId;
        context.Add(order);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return eventId;
    }

    private ShopDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ShopDbContext>()
            .UseSqlite(_database.ConnectionString)
            .ReplaceService<IModelCustomizer, UtcTicksModelCustomizer>()
            .Options);

    private async Task<OutboxRow> MessageAsync(Guid eventId)
    {
        await using var context = CreateContext();
        return await context.Database
            .SqlQueryRaw<OutboxRow>(
                "SELECT EventId, Attempts, Error, ProcessedOnUtc FROM outbox_messages WHERE EventId = {0}", eventId)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    // A message processed at processedOnUtc, with one handler's record, as an earlier pass would have left it.
    private async Task<Guid> AddProcessedMessageAsync(DateTimeOffset processedOnUtc, string handler)
    {
        var eventId = Guid.CreateVersion7();
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO outbox_messages (EventId, Type, Content, OccurredOnUtc, ProcessedOnUtc, Attempts) VALUES ({0}, {1}, {2}, {3}, {3}, 0)",
            [eventId, "System.String, System.Private.CoreLib", "\"text\"", processedOnUtc.UtcTicks],
            TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO outbox_consumers (MessageId, Handler, ProcessedOnUtc) SELECT Id, {1}, {2} FROM outbox_messages WHERE EventId = {0}",
            [eventId, handler, processedOnUtc.UtcTicks],
            TestContext.Current.CancellationToken);
        return eventId;
    }

    private async Task<bool> MessageExistsAsync(Guid eventId)
    {
        await using var context = CreateContext();
        return await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM outbox_messages WHERE EventId = {0}", eventId)
            .SingleAsync(TestContext.Current.CancellationToken) == 1;
    }

    // A handler's record belongs to the message row (MessageId), not to the event id.
    private async Task<List<string>> ConsumersAsync(Guid eventId)
    {
        await using var context = CreateContext();
        return await context.Database
            .SqlQueryRaw<string>(
                "SELECT c.Handler AS Value FROM outbox_consumers c JOIN outbox_messages m ON m.Id = c.MessageId WHERE m.EventId = {0}",
                eventId)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<int> ConsumerCountAsync()
    {
        await using var context = CreateContext();
        return await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM outbox_consumers")
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    // The dispatcher runs in the background: poll its visible effect until it shows up.
    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        while (!await condition())
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
    }

    private sealed record OrderPlaced(Guid OrderId) : DomainEvent;

    // ProcessedOnUtc in UTC ticks, as the test databases store it (UtcTicksModelCustomizer).
    private sealed record OutboxRow(Guid EventId, int Attempts, string? Error, long? ProcessedOnUtc);

    private sealed class Order : Entity<Guid>
    {
        // EF Core's materialization ctor.
        private Order() { }

        public Order(Guid id) : base(id) => Raise(new OrderPlaced(id));
    }

    private sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>().Ignore(order => order.DomainEvents);
            modelBuilder.AddOutbox();
        }
    }

    // Every handler call, in order, across hosts.
    private sealed class HandlerLog
    {
        private readonly Lock _gate = new();
        private readonly List<(string Handler, Guid EventId)> _calls = [];

        public IReadOnlyList<(string Handler, Guid EventId)> Calls
        {
            get
            {
                lock (_gate)
                    return [.. _calls];
            }
        }

        public int Record(string handler, Guid eventId)
        {
            lock (_gate)
            {
                _calls.Add((handler, eventId));
                return _calls.Count(call => call == (handler, eventId));
            }
        }
    }

    private sealed class RecordingHandler(HandlerLog log) : IDomainEventHandler<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default)
        {
            log.Record(nameof(RecordingHandler), domainEvent.EventId);
            return Task.CompletedTask;
        }
    }

    private sealed class FailsOnceHandler(HandlerLog log) : IDomainEventHandler<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default) =>
            log.Record(nameof(FailsOnceHandler), domainEvent.EventId) == 1
                ? throw new InvalidOperationException("The first delivery fails.")
                : Task.CompletedTask;
    }

    private sealed class AlwaysFailsHandler(HandlerLog log) : IDomainEventHandler<OrderPlaced>
    {
        public const string Failure = "This handler always fails.";

        public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default)
        {
            log.Record(nameof(AlwaysFailsHandler), domainEvent.EventId);
            throw new InvalidOperationException(Failure);
        }
    }

    // A closed generic handler's full name carries its type argument's assembly-qualified name, version included.
    private sealed class GenericHandler<TMarker>(HandlerLog log) : IDomainEventHandler<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default)
        {
            log.Record(nameof(GenericHandler<TMarker>), domainEvent.EventId);
            return Task.CompletedTask;
        }
    }

    private sealed class AMarkerWhoseNameMakesTheClosedGenericHandlerNameLongerThanTheTwoHundredFiftySixCharactersItsRecordKeeps;

    // Waits for the host to stop, then fails with an exception of its own, as a library cut short might.
    private sealed class FailsWhenStoppedHandler(HandlerLog log) : IDomainEventHandler<OrderPlaced>
    {
        public async Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default)
        {
            log.Record(nameof(FailsWhenStoppedHandler), domainEvent.EventId);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException exception)
            {
                throw new InvalidOperationException("The delivery was cut short.", exception);
            }
        }
    }

    // Retries only the error the interceptor below raises, straight away.
    private sealed class RetryingStrategy(ExecutionStrategyDependencies dependencies)
        : ExecutionStrategy(dependencies, maxRetryCount: 3, maxRetryDelay: TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TransientException;
    }

    private sealed class TransientException() : Exception("A transient failure.");

    private sealed class FailFirstConsumerInsert : DbCommandInterceptor
    {
        private int _failed;

        public bool Failed => Volatile.Read(ref _failed) == 1;

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            FailOnce(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            FailOnce(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void FailOnce(DbCommand command)
        {
            if (command.CommandText.Contains("INSERT INTO \"outbox_consumers\"", StringComparison.Ordinal)
                && Interlocked.Exchange(ref _failed, 1) == 0)
                throw new TransientException();
        }
    }
}
