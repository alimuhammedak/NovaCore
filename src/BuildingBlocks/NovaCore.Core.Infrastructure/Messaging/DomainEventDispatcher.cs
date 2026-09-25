using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Messaging;

namespace NovaCore.Core.Infrastructure.Messaging;

/// <summary>
/// In-process domain-event dispatcher. Resolves every <see cref="IDomainEventHandler{TEvent}"/> for
/// each event from DI and invokes it. No third-party mediator, no reflection per call beyond the
/// handler type lookup — and the seam can later be swapped for an out-of-process bus.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    /// <inheritdoc />
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null)
                    continue;

                var task = (Task)handlerType
                    .GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!
                    .Invoke(handler, [domainEvent, cancellationToken])!;

                await task;
            }
        }
    }
}
