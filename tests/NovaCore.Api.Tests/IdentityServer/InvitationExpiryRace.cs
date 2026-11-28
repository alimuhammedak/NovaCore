using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Time.Testing;
using NovaCore.Identity.OpenIddict;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Lets an invitation expire while it is being accepted. Once armed, the next transaction that starts on a context
/// tracking an invitation first moves the clock on: the accept has found the invitation pending and passed the password
/// policy, and opens the transaction that marks it after the invitation's end.
/// </summary>
/// <remarks>
/// The tracked invitation tells the accept's transaction from the background ones (the token pruner's): only the
/// accept's context has loaded an invitation. One shot: it disarms itself when it moves the clock.
/// </remarks>
public sealed class InvitationExpiryRace(FakeTimeProvider clock) : DbTransactionInterceptor
{
    // In ticks; zero is disarmed.
    private long _advance;

    /// <summary>Moves the clock by <paramref name="advance"/> when the next accept opens its transaction.</summary>
    public void Arm(TimeSpan advance) => Interlocked.Exchange(ref _advance, advance.Ticks);

    public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context?.ChangeTracker.Entries<Invitation>().Any() == true
            && Interlocked.Exchange(ref _advance, 0) is var advance and > 0)
            clock.Advance(TimeSpan.FromTicks(advance));

        return base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
    }
}
