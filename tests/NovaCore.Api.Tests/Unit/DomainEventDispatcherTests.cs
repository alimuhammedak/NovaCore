using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaCore.Abstractions.Messaging;
using NovaCore.EntityFrameworkCore.Persistence;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// PLAN F4a.3: the in-process dispatcher resolves the handlers from the caller's scope. It used to be a singleton that
/// resolved them from the root provider, which scope validation rejects for scoped handlers.
/// </summary>
public class DomainEventDispatcherTests
{
    [Fact]
    public async Task Handlers_run_in_the_callers_scope()
    {
        using var host = BuildHost(services => services
            .AddScoped<UnitOfWork>()
            .AddScoped<IDomainEventHandler<OrderPlaced>, PlacedHandler>());
        await using var scope = host.Services.CreateAsyncScope();
        var placed = new OrderPlaced(Guid.NewGuid());

        await scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>()
            .DispatchAsync([placed], TestContext.Current.CancellationToken);

        // The handler wrote to the caller's UnitOfWork, not to one of its own.
        Assert.Equal([(nameof(PlacedHandler), placed)], scope.ServiceProvider.GetRequiredService<UnitOfWork>().Handled);
    }

    [Fact]
    public async Task Every_handler_of_an_event_runs_and_no_other()
    {
        using var host = BuildHost(services => services
            .AddScoped<UnitOfWork>()
            .AddScoped<IDomainEventHandler<OrderPlaced>, PlacedHandler>()
            .AddScoped<IDomainEventHandler<OrderPlaced>, PlacedAuditHandler>()
            .AddScoped<IDomainEventHandler<OrderCancelled>, CancelledHandler>());
        await using var scope = host.Services.CreateAsyncScope();
        var placed = new OrderPlaced(Guid.NewGuid());

        await scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>()
            .DispatchAsync([placed], TestContext.Current.CancellationToken);

        Assert.Equal(
            [(nameof(PlacedHandler), placed), (nameof(PlacedAuditHandler), placed)],
            scope.ServiceProvider.GetRequiredService<UnitOfWork>().Handled);
    }

    [Fact]
    public async Task An_event_without_handlers_is_ignored()
    {
        using var host = BuildHost(services => services.AddScoped<UnitOfWork>());
        await using var scope = host.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>()
            .DispatchAsync([new OrderCancelled(Guid.NewGuid())], TestContext.Current.CancellationToken);

        Assert.Empty(scope.ServiceProvider.GetRequiredService<UnitOfWork>().Handled);
    }

    private static IHost BuildHost(Action<IServiceCollection> registerHandlers)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddCorePersistence();
        registerHandlers(builder.Services);
        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateScopes = true }));
        return builder.Build();
    }

    private sealed record OrderPlaced(Guid OrderId) : DomainEvent;

    private sealed record OrderCancelled(Guid OrderId) : DomainEvent;

    // Scoped state the handlers share with their caller, like a request's DbContext.
    private sealed class UnitOfWork
    {
        public List<(string Handler, IDomainEvent Event)> Handled { get; } = [];
    }

    private sealed class PlacedHandler(UnitOfWork unitOfWork) : IDomainEventHandler<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default)
        {
            unitOfWork.Handled.Add((nameof(PlacedHandler), domainEvent));
            return Task.CompletedTask;
        }
    }

    private sealed class PlacedAuditHandler(UnitOfWork unitOfWork) : IDomainEventHandler<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced domainEvent, CancellationToken cancellationToken = default)
        {
            unitOfWork.Handled.Add((nameof(PlacedAuditHandler), domainEvent));
            return Task.CompletedTask;
        }
    }

    private sealed class CancelledHandler(UnitOfWork unitOfWork) : IDomainEventHandler<OrderCancelled>
    {
        public Task HandleAsync(OrderCancelled domainEvent, CancellationToken cancellationToken = default)
        {
            unitOfWork.Handled.Add((nameof(CancelledHandler), domainEvent));
            return Task.CompletedTask;
        }
    }
}
