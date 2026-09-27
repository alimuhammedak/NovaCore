using NovaCore.Abstractions.Messaging;

namespace NovaCore.EntityFrameworkCore.Messaging;

/// <summary>
/// In-process domain-event dispatch: resolves every <see cref="IDomainEventHandler{TEvent}"/> of each event from the
/// caller's scope and runs them now, one after another. Registered scoped, so the handlers share the caller's scoped
/// services (its DbContext, for instance).
/// </summary>
// The service provider is the caller's scope: handlers are found by the event's runtime type, which DI can't inject
// (a narrow infrastructure exception to Y-25).
internal sealed class DomainEventDispatcher(IServiceProvider services, DomainEventHandlerInvokers invokers)
    : IDomainEventDispatcher
{
    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var invoker = invokers.For(domainEvent.GetType());
            foreach (var handler in invoker.GetHandlers(services))
                await invoker.InvokeAsync(handler, domainEvent, cancellationToken);
        }
    }
}
