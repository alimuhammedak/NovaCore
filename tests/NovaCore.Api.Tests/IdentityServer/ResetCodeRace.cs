using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Plays many requests guessing one password reset code at the same time. Once armed for <c>n</c> requests, every save
/// that writes a user's <c>[NovaCore]/PasswordResetCode</c> or <c>PasswordResetCodeFailures</c> token waits until <c>n</c>
/// saves are waiting: all the requests read the code before any of them writes, on every run, instead of when the
/// scheduler happens to interleave them (like <see cref="UsedCodeRace"/>).
/// </summary>
public sealed class ResetCodeRace : SaveChangesInterceptor
{
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(30);

    private readonly Lock _gate = new();
    private int _waiting;
    private int _expected;
    private TaskCompletionSource? _release;
    private Func<Task>? _competitor;

    /// <summary>Holds the next saves of reset code tokens until <paramref name="requests"/> of them are waiting.</summary>
    public void Arm(int requests)
    {
        lock (_gate)
        {
            _expected = requests;
            _waiting = 0;
            _release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    /// <summary>
    /// Runs <paramref name="competitor"/> (once) to its end before the next save that writes an account's run of wrong
    /// codes (<c>PasswordResetCodeFailures</c>): the competitor reads the account after every earlier save of this
    /// request and writes before this one.
    /// </summary>
    public void RunBeforeNextFailuresSave(Func<Task> competitor)
    {
        lock (_gate)
            _competitor = competitor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (TakeCompetitor(eventData.Context!) is { } competitor)
            await competitor();

        // Fewer requests than armed for never reach the gate together: the wait fails the save loudly instead of hanging.
        await Join(eventData.Context!).WaitAsync(MaxWait, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // The competitor's own saves come through here too, disarmed by then.
    private Func<Task>? TakeCompetitor(DbContext context)
    {
        lock (_gate)
        {
            if (_competitor is null || !Writes(context, "PasswordResetCodeFailures"))
                return null;

            var competitor = _competitor;
            _competitor = null;
            return competitor;
        }
    }

    private static bool Writes(DbContext context, params string[] names) =>
        context.ChangeTracker.Entries<IdentityUserToken<Guid>>().Any(entry =>
            entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
            && string.Equals(entry.Entity.LoginProvider, "[NovaCore]", StringComparison.Ordinal)
            && names.Contains(entry.Entity.Name, StringComparer.Ordinal));

    // The saves after the release come through here too, disarmed by then.
    private Task Join(DbContext context)
    {
        lock (_gate)
        {
            if (_release is null || !Writes(context, "PasswordResetCode", "PasswordResetCodeFailures"))
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
