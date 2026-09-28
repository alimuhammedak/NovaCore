using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Messaging;
using NovaCore.Modules.Catalog.Domain;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Payments.Contracts;
using NovaCore.Modules.Payments.Domain;
using NovaCore.Modules.Payments.Infrastructure;

namespace NovaCore.Api.Tests;

/// <summary>
/// Catalog and Payments data for the stock reservation tests, read and written through the host's own module contexts,
/// each call in a scope of its own.
/// </summary>
internal static class ModuleTestData
{
    public static async Task<Guid> AddProductAsync(this NovaCoreApiFactory factory, int stock, bool active = true)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = new Product("Widget", null, 10m, stock);
        if (!active)
            product.Deactivate();
        catalog.Products.Add(product);
        await catalog.SaveChangesAsync();
        return product.Id;
    }

    /// <summary>
    /// Saves a pending payment as <c>CreatePayment</c> does, without its checks: its <see cref="PaymentCreated"/> waits in
    /// the Payments outbox for the dispatcher.
    /// </summary>
    public static async Task<Guid> AddPaymentAsync(this NovaCoreApiFactory factory, Guid productId, int quantity)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var payment = new Payment(productId, "Widget", quantity, 10m);
        payments.Payments.Add(payment);
        await payments.SaveChangesAsync();
        return payment.Id;
    }

    /// <summary>
    /// Saves a pending payment and returns its <see cref="PaymentCreated"/> for the test to hand to a handler itself. The
    /// request is kept out of the outbox, so the dispatcher never delivers it at the same time.
    /// </summary>
    public static async Task<PaymentCreated> AddPaymentForDirectHandlingAsync(
        this NovaCoreApiFactory factory, Guid productId, int quantity)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var payments = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var payment = new Payment(productId, "Widget", quantity, 10m);
        var request = (PaymentCreated)payment.DomainEvents.Single();
        payment.ClearDomainEvents();
        payments.Payments.Add(payment);
        await payments.SaveChangesAsync();
        return request;
    }

    /// <summary>Hands <paramref name="domainEvent"/> to its handler in a scope of its own, as the outbox dispatcher does.</summary>
    public static async Task HandleAsync<TEvent>(this NovaCoreApiFactory factory, TEvent domainEvent)
        where TEvent : IDomainEvent
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IDomainEventHandler<TEvent>>().HandleAsync(domainEvent);
    }

    public static async Task<int> StockAsync(this NovaCoreApiFactory factory, Guid productId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Products
            .Where(product => product.Id == productId)
            .Select(product => product.Stock)
            .SingleAsync();
    }

    public static async Task<StockReservation> ReservationAsync(this NovaCoreApiFactory factory, Guid paymentId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().StockReservations
            .AsNoTracking()
            .SingleAsync(reservation => reservation.Id == paymentId);
    }

    // From the database: the payment list orders by a DateTimeOffset, and the SQLite test database can't order by one.
    public static async Task<PaymentStatus> PaymentStatusAsync(this NovaCoreApiFactory factory, Guid paymentId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Payments
            .Where(payment => payment.Id == paymentId)
            .Select(payment => payment.Status)
            .SingleAsync();
    }

    /// <summary>
    /// The <typeparamref name="TEvent"/> messages naming <paramref name="paymentId"/> in the outbox of the module that owns
    /// <typeparamref name="TContext"/>, waiting or delivered.
    /// </summary>
    public static async Task<List<OutboxRow>> OutboxMessagesAsync<TContext, TEvent>(
        this NovaCoreApiFactory factory, Guid paymentId)
        where TContext : DbContext
        where TEvent : IDomainEvent
    {
        var type = typeof(TEvent);
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<TContext>().Database
            .SqlQueryRaw<OutboxRow>(
                "SELECT Attempts, Error FROM outbox_messages WHERE Type = {0} AND Content LIKE {1}",
                $"{type.FullName}, {type.Assembly.GetName().Name}", $"%{paymentId}%")
            .ToListAsync();
    }
}

/// <summary>An outbox message's delivery record.</summary>
internal sealed record OutboxRow(int Attempts, string? Error);
