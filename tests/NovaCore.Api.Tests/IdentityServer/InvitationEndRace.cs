using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Time.Testing;
using NovaCore.Identity.OpenIddict;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Ends an invitation while it is being accepted. Once armed, the next transaction that starts on a context tracking an
/// invitation first ends it: the accept has found the invitation pending and passed the password policy, and opens the
/// transaction that marks it after its end. <see cref="ArmExpiry"/> moves the clock past the end;
/// <see cref="ArmDeletion"/> deletes the invitation rows on the transaction's connection (V10-2).
/// </summary>
/// <remarks>
/// The tracked invitation tells the accept's transaction from the background ones (the token pruner's): only the
/// accept's context has loaded an invitation. One shot: it disarms itself when it ends the invitation. The deletion is
/// SQLite's: the test host's database has no schemas.
/// </remarks>
public sealed class InvitationEndRace(FakeTimeProvider? clock = null) : DbTransactionInterceptor
{
    // In ticks; zero is disarmed.
    private long _advance;
    private int _delete;

    /// <summary>Moves the clock by <paramref name="advance"/> when the next accept opens its transaction.</summary>
    public void ArmExpiry(TimeSpan advance) => Interlocked.Exchange(ref _advance, advance.Ticks);

    /// <summary>Deletes the invitations when the next accept opens its transaction.</summary>
    public void ArmDeletion() => Interlocked.Exchange(ref _delete, 1);

    public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context?.ChangeTracker.Entries<Invitation>().Any() == true)
        {
            if (Interlocked.Exchange(ref _advance, 0) is var advance and > 0)
                clock!.Advance(TimeSpan.FromTicks(advance));

            if (Interlocked.Exchange(ref _delete, 0) == 1)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM invitations";
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        return await base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
    }
}
