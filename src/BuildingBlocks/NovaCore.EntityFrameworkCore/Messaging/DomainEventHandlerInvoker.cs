using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Messaging;

namespace NovaCore.EntityFrameworkCore.Messaging;

/// <summary>
/// Resolves and runs the handlers of one event type, with the type known at compile time: dispatching an event costs no
/// reflection. <see cref="DomainEventHandlerInvokers"/> builds one per event type.
/// </summary>
internal abstract class DomainEventHandlerInvoker
{
    /// <summary>The event type's handlers, in registration order, resolved from <paramref name="services"/>.</summary>
    public abstract IReadOnlyList<object> GetHandlers(IServiceProvider services);

    /// <summary>Runs one of the handlers <see cref="GetHandlers"/> returned.</summary>
    public abstract Task InvokeAsync(object handler, IDomainEvent domainEvent, CancellationToken cancellationToken);
}

/// <inheritdoc />
internal sealed class DomainEventHandlerInvoker<TEvent> : DomainEventHandlerInvoker
    where TEvent : IDomainEvent
{
    /// <inheritdoc />
    public override IReadOnlyList<object> GetHandlers(IServiceProvider services) =>
        [.. services.GetServices<IDomainEventHandler<TEvent>>()];

    /// <inheritdoc />
    public override Task InvokeAsync(object handler, IDomainEvent domainEvent, CancellationToken cancellationToken) =>
        ((IDomainEventHandler<TEvent>)handler).HandleAsync((TEvent)domainEvent, cancellationToken);
}

/// <summary>
/// The invoker of each event type, created once (the only reflection) and kept for the host's lifetime. A singleton in DI
/// rather than static state.
/// </summary>
internal sealed class DomainEventHandlerInvokers
{
    private readonly ConcurrentDictionary<Type, DomainEventHandlerInvoker> _invokers = new();

    /// <summary>The invoker for <paramref name="eventType"/>, which must implement <see cref="IDomainEvent"/>.</summary>
    public DomainEventHandlerInvoker For(Type eventType) =>
        _invokers.GetOrAdd(eventType, static type => (DomainEventHandlerInvoker)Activator.CreateInstance(
            typeof(DomainEventHandlerInvoker<>).MakeGenericType(type))!);
}
