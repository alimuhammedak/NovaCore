using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NovaCore.Identity.OpenIddict;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Plays another request writing the account (a failed sign-in counting toward lockout, say) while a password is being
/// set. Once armed, the next save that changes a user's password hash first gives that user a new concurrency stamp in
/// the database, through the saving context (so inside its transaction, if it has one): the save then fails like one
/// that lost to a concurrent write, on every run.
/// </summary>
public sealed class ConcurrentUserWrite : SaveChangesInterceptor
{
    private int _armed;

    /// <summary>Makes the next save of a new password lose to a concurrent write of the account.</summary>
    public void Arm() => Volatile.Write(ref _armed, 1);

    // UserStore.UpdateAsync marks every property modified, so a new password shows as a changed value, not a flag.
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context!;
        var user = context.ChangeTracker.Entries<ApplicationUser>()
            .FirstOrDefault(entry => entry.State == EntityState.Modified && !string.Equals(
                entry.Property(stored => stored.PasswordHash).OriginalValue, entry.Property(stored => stored.PasswordHash).CurrentValue, StringComparison.Ordinal));
        if (user is not null && Interlocked.Exchange(ref _armed, 0) == 1)
        {
            var id = user.Entity.Id;
            await context.Set<ApplicationUser>().Where(stored => stored.Id == id)
                .ExecuteUpdateAsync(set => set.SetProperty(stored => stored.ConcurrencyStamp, Guid.NewGuid().ToString()), cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
