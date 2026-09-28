using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace NovaCore.Api.Tests;

/// <summary>
/// Drives the host's outbox dispatcher on a fake clock: every advance past its polling interval makes a pass. It keeps
/// advancing until the expected effect shows up (tests/CLAUDE.md): an advance made before the dispatcher's timer exists is
/// lost, and one advance can make two passes.
/// </summary>
internal static class OutboxDelivery
{
    // OutboxDispatcher.PollingInterval, which is internal to NovaCore.EntityFrameworkCore.
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public static async Task RunUntilAsync(FakeTimeProvider clock, Func<Task<bool>> delivered)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            while (!await delivered())
            {
                clock.Advance(PollingInterval);
                await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);
            }
        }
        catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The outbox didn't deliver the expected effect within {Timeout.TotalSeconds} seconds.");
        }
    }
}
