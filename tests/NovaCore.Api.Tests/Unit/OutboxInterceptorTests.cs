using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Entities;
using NovaCore.Abstractions.Messaging;
using NovaCore.Abstractions.MultiTenancy;
using NovaCore.EntityFrameworkCore.Messaging;
using NovaCore.EntityFrameworkCore.Persistence;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// PLAN F4a.1: the events an entity raises are written to its module's <c>outbox_messages</c> table by the SaveChanges
/// that saves the entity, so both are committed or neither is. Module contexts get the interceptor from
/// <c>AddModuleDbContext</c>. PLAN F7b.5: a message keeps the tenant of the scope that saved it and the trace context of
/// the activity it was saved in, for its handlers.
/// </summary>
public sealed class OutboxInterceptorTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public OutboxInterceptorTests() => _connection.Open();

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Raised_events_are_saved_to_the_outbox_with_the_entity()
    {
        await using var services = await CreateServicesAsync<ShopDbContext>();
        var order = new Order(Guid.CreateVersion7(), eventId: Guid.CreateVersion7());
        var placed = Assert.IsType<OrderPlaced>(Assert.Single(order.DomainEvents));

        await SaveAsync<ShopDbContext>(services, order);

        Assert.Empty(order.DomainEvents);
        Assert.Equal([order.Id], await OrderIdsAsync(services));
        var message = Assert.Single(await OutboxAsync(services));
        Assert.Equal(placed.EventId, message.EventId);
        Assert.Equal(placed.OccurredOnUtc, message.OccurredOnUtc);
        Assert.Equal(typeof(OrderPlaced), Type.GetType(message.Type));
        Assert.Equal(placed, JsonSerializer.Deserialize<OrderPlaced>(message.Content));
    }

    [Fact]
    public async Task A_message_keeps_the_saving_scopes_tenant_and_the_saving_activitys_trace_context()
    {
        await using var services = await CreateServicesAsync<ShopDbContext>();
        var tenant = Guid.NewGuid();
        using var request = new Activity("request");
        request.SetIdFormat(ActivityIdFormat.W3C).Start();

        await SaveAsync<ShopDbContext>(services, new Order(Guid.CreateVersion7(), Guid.CreateVersion7()), tenant);

        var message = Assert.Single(await OutboxAsync(services));
        Assert.Equal(tenant, message.TenantId);
        Assert.Equal(request.Id, message.TraceParent);
    }

    [Fact]
    public async Task A_message_saved_without_a_tenant_or_an_activity_keeps_neither()
    {
        await using var services = await CreateServicesAsync<ShopDbContext>();

        await SaveAsync<ShopDbContext>(services, new Order(Guid.CreateVersion7(), Guid.CreateVersion7()));

        var message = Assert.Single(await OutboxAsync(services));
        Assert.Null(message.TenantId);
        Assert.Null(message.TraceParent);
    }

    [Fact]
    public async Task A_message_keeps_no_trace_context_of_an_activity_that_isnt_w3c()
    {
        await using var services = await CreateServicesAsync<ShopDbContext>();
        using var request = new Activity("request");
        request.SetIdFormat(ActivityIdFormat.Hierarchical).Start();

        await SaveAsync<ShopDbContext>(services, new Order(Guid.CreateVersion7(), Guid.CreateVersion7()));

        Assert.Null(Assert.Single(await OutboxAsync(services)).TraceParent);
    }

    // The two tests below fail one insert of a save each: whichever insert runs first, the other is rolled back with it.
    [Fact]
    public async Task A_rejected_outbox_row_rolls_back_the_entity()
    {
        await using var services = await CreateServicesAsync<ShopDbContext>();
        var eventId = Guid.CreateVersion7();
        await SaveAsync<ShopDbContext>(services, new Order(Guid.CreateVersion7(), eventId));

        // An event id is unique in the outbox.
        var second = new Order(Guid.CreateVersion7(), eventId);
        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync<ShopDbContext>(services, second));

        Assert.DoesNotContain(second.Id, await OrderIdsAsync(services));
        Assert.Single(await OutboxAsync(services));
    }

    [Fact]
    public async Task A_rejected_entity_rolls_back_its_outbox_row()
    {
        await using var services = await CreateServicesAsync<ShopDbContext>();
        var orderId = Guid.CreateVersion7();
        var firstEventId = Guid.CreateVersion7();
        await SaveAsync<ShopDbContext>(services, new Order(orderId, firstEventId));

        // Same key: the order insert fails.
        await Assert.ThrowsAsync<DbUpdateException>(
            () => SaveAsync<ShopDbContext>(services, new Order(orderId, Guid.CreateVersion7())));

        Assert.Equal(firstEventId, Assert.Single(await OutboxAsync(services)).EventId);
    }

    [Fact]
    public async Task Events_raised_in_a_context_without_an_outbox_fail_the_save()
    {
        await using var services = await CreateServicesAsync<NoOutboxDbContext>();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SaveAsync<NoOutboxDbContext>(services, new Order(Guid.CreateVersion7(), Guid.CreateVersion7())));

        Assert.Contains("AddOutbox", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_context_without_an_outbox_saves_entities_that_raised_nothing()
    {
        await using var services = await CreateServicesAsync<NoOutboxDbContext>();
        var order = new Order(Guid.CreateVersion7(), Guid.CreateVersion7());
        order.ClearDomainEvents();

        await SaveAsync<NoOutboxDbContext>(services, order);

        await using var scope = services.CreateAsyncScope();
        Assert.True(await scope.ServiceProvider.GetRequiredService<NoOutboxDbContext>().Orders
            .AnyAsync(saved => saved.Id == order.Id, TestContext.Current.CancellationToken));
    }

    private async Task<ServiceProvider> CreateServicesAsync<TContext>()
        where TContext : DbContext
    {
        var services = new ServiceCollection()
            .AddModuleDatabase((_, options) => options.UseSqlite(_connection))
            .AddModuleDbContext<TContext>("shop", (_, _) => { })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TContext>().Database
            .EnsureCreatedAsync(TestContext.Current.CancellationToken);

        return services;
    }

    private static async Task SaveAsync<TContext>(IServiceProvider services, Order order, Guid? tenant = null)
        where TContext : DbContext
    {
        await using var scope = services.CreateAsyncScope();
        if (tenant is { } id)
            scope.ServiceProvider.GetRequiredService<TenantContext>().Use(id);

        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        context.Add(order);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<List<Guid>> OrderIdsAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Orders
            .Select(order => order.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<List<OutboxRow>> OutboxAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database
            .SqlQueryRaw<OutboxRow>(
                "SELECT EventId, Type, Content, OccurredOnUtc, TenantId, TraceParent FROM outbox_messages ORDER BY Id")
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private sealed record OrderPlaced(Guid OrderId) : DomainEvent;

    private sealed record OutboxRow(
        Guid EventId, string Type, string Content, DateTimeOffset OccurredOnUtc, Guid? TenantId, string? TraceParent);

    private sealed class Order : Entity<Guid>
    {
        // EF Core's materialization ctor.
        private Order() { }

        public Order(Guid id, Guid eventId) : base(id) => Raise(new OrderPlaced(id) { EventId = eventId });
    }

    private sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>().Ignore(order => order.DomainEvents);
            modelBuilder.AddOutbox();
        }
    }

    private sealed class NoOutboxDbContext(DbContextOptions<NoOutboxDbContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Order>().Ignore(order => order.DomainEvents);
    }
}
