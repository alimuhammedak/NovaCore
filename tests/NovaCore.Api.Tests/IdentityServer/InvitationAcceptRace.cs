using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Plays requests accepting one invitation at the same time. Once armed for <c>n</c> requests, the next transactions to
/// start wait until <c>n</c> of them are waiting: every request has found the invitation pending and passed the password
/// policy before any of them opens the transaction that marks it, on every run (like <see cref="ResetCodeRace"/>).
/// </summary>
/// <remarks>
/// It holds the transaction's start, not the conditional update: SQLite's transaction (<c>BEGIN IMMEDIATE</c>) takes the
/// write lock at its start, so a request held after it would keep the other from ever reaching the gate. Nothing else
/// starts a transaction between an accept's lookup and its mark, so armed, the gate sees only the accepts.
/// </remarks>
public sealed class InvitationAcceptRace : DbTransactionInterceptor
{
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(30);

    private readonly Lock _gate = new();
    private int _waiting;
    private int _expected;
    private TaskCompletionSource? _release;

    /// <summary>Holds the next transactions until <paramref name="requests"/> of them are waiting to start.</summary>
    public void Arm(int requests)
    {
        lock (_gate)
        {
            _expected = requests;
            _waiting = 0;
            _release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(
        DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default)
    {
        // Fewer requests than armed for never reach the gate together: the wait fails the request loudly instead of hanging.
        await Join().WaitAsync(MaxWait, cancellationToken);

        return await base.TransactionStartingAsync(connection, eventData, result, cancellationToken);
    }

    private Task Join()
    {
        lock (_gate)
        {
            if (_release is null)
                return Task.CompletedTask;

            var release = _release;
            if (++_waiting == _expected)
            {
                _release = null;
                release.SetResult();
            }

            return release.Task;
        }
    }
}
