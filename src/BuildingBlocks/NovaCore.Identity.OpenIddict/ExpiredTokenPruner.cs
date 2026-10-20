using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Deletes the OpenIddict tokens and authorizations nobody can use any more, so the store doesn't grow with every sign-in
/// and refresh: a pass when the host starts, then one every <see cref="Interval"/>. OpenIddict's <c>PruneAsync</c> decides
/// what goes, among the rows created more than <see cref="MinimumLifespan"/> ago: the tokens that are expired, revoked,
/// redeemed or of an authorization that is no longer valid, then the authorizations that are no longer valid (or ad hoc)
/// and have no token left. A valid permanent authorization, the kind the provider creates, stays.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>The interval and the lifespan are OpenIddict.Quartz's defaults (docs/plan/RESEARCH.md R-088). The lifespan is
/// longer than a refresh token's 7 days, so a redeemed refresh token is still found when it is reused (chain revocation).
/// They are constants: no consumer needs them configurable yet (Y-19).</item>
/// <item>The threshold comes from <see cref="TimeProvider"/>; OpenIddict compares the expiration with the database's
/// clock.</item>
/// <item>Several hosts over one store may prune at the same time: deleting the same rows twice is harmless.</item>
/// </list>
/// </remarks>
internal sealed partial class ExpiredTokenPruner(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<ExpiredTokenPruner> logger) : BackgroundService
{
    /// <summary>How long the pruner waits between passes.</summary>
    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    /// <summary>How old a token or an authorization must be before it can be pruned.</summary>
    public static readonly TimeSpan MinimumLifespan = TimeSpan.FromDays(14);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // A PeriodicTimer keeps a tick that fires during a pass, so no interval is lost.
        using var timer = new PeriodicTimer(Interval, timeProvider);
        do
        {
            await PruneAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    // Tokens first: an authorization with a token left is never pruned. A failed token step doesn't skip the
    // authorizations, whose own rows may still be prunable (OpenIddict.Quartz goes on the same way).
    private async Task PruneAsync(CancellationToken cancellationToken)
    {
        var threshold = timeProvider.GetUtcNow() - MinimumLifespan;

        var tokens = await PruneAsync<IOpenIddictTokenManager>(
            "tokens", (manager, token) => manager.PruneAsync(threshold, token), cancellationToken);
        var authorizations = await PruneAsync<IOpenIddictAuthorizationManager>(
            "authorizations", (manager, token) => manager.PruneAsync(threshold, token), cancellationToken);

        if (tokens + authorizations > 0)
            LogPruned(tokens, authorizations);
    }

    private async Task<long> PruneAsync<TManager>(
        string records, Func<TManager, CancellationToken, ValueTask<long>> prune, CancellationToken cancellationToken)
        where TManager : notnull
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            return await prune(scope.ServiceProvider.GetRequiredService<TManager>(), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // The loop's boundary: an unreachable database or a missing table must not stop the host. Only the host's own
            // stop gets through.
            LogPassFailed(records, exception);
            return 0;
        }
    }

    [LoggerMessage(EventId = 41, Level = LogLevel.Information, Message = "Pruned {Tokens} tokens and {Authorizations} authorizations of the identity provider.")]
    private partial void LogPruned(long tokens, long authorizations);

    [LoggerMessage(EventId = 42, Level = LogLevel.Error, Message = "Pruning the identity provider's {Records} failed. The next pass tries again.")]
    private partial void LogPassFailed(string records, Exception exception);
}
