using NovaCore.Abstractions.Messaging;
using Xunit;

namespace NovaCore.Domain.Tests;

public class DomainEventTests
{
    private sealed record SampleEvent(Guid OrderId) : DomainEvent;

    [Fact]
    public void Identity_and_timestamp_are_fixed_when_the_event_is_created()
    {
        IDomainEvent domainEvent = new SampleEvent(Guid.NewGuid());

        Assert.Equal(domainEvent.EventId, domainEvent.EventId);
        Assert.Equal(domainEvent.OccurredOnUtc, domainEvent.OccurredOnUtc);
    }
}
