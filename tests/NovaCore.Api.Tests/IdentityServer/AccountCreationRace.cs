using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NovaCore.Identity.OpenIddict;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Plays requests creating one account at the same time. Once armed for <c>n</c> requests, every save that adds an
/// <see cref="ApplicationUser"/> waits until <c>n</c> saves are waiting: every request has passed Identity's user
/// validation (no account yet) before any of them inserts, on every run (like <see cref="ResetCodeRace"/>).
/// </summary>
public sealed class AccountCreationRace : SaveChangesInterceptor
{
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(30);

    private readonly Lock _gate = new();
    private int _waiting;
    private int _expected;
    private TaskCompletionSource? _release;

    /// <summary>Holds the next saves that add an account until <paramref name="requests"/> of them are waiting.</summary>
    public void Arm(int requests)
    {
        lock (_gate)
        {
            _expected = requests;
            _waiting = 0;
            _release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        // Fewer requests than armed for never reach the gate together: the wait fails the save loudly instead of hanging.
        await Join(eventData.Context!).WaitAsync(MaxWait, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private Task Join(DbContext context)
    {
        lock (_gate)
        {
            if (_release is null || !context.ChangeTracker.Entries<ApplicationUser>().Any(entry => entry.State == EntityState.Added))
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
