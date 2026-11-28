using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NovaCore.Identity.OpenIddict;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Like <see cref="ConcurrentUserWrite"/>, for a save of one of the reset codes' user tokens rather than a new password:
/// once armed for a user and a token, the next save that adds, changes or removes that user's
/// <c>[NovaCore]/&lt;token&gt;</c> first gives the user a new concurrency stamp in the database, through the saving
/// context, so the save fails like one that lost to a concurrent write, on every run. Armed per user, so a save that
/// never comes (the code under test no longer saves through the change tracker) can't fire in another test.
/// </summary>
public sealed class ConcurrentTokenWrite : SaveChangesInterceptor
{
    private readonly Lock _gate = new();
    private (Guid UserId, string Token)? _armed;

    /// <summary>Makes the next save of <paramref name="userId"/>'s <paramref name="token"/> lose to a concurrent write.</summary>
    public void Arm(Guid userId, string token)
    {
        lock (_gate)
            _armed = (userId, token);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context!;
        if (Take(context) is { } userId)
        {
            await context.Set<ApplicationUser>().Where(stored => stored.Id == userId)
                .ExecuteUpdateAsync(set => set.SetProperty(stored => stored.ConcurrencyStamp, Guid.NewGuid().ToString()), cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private Guid? Take(DbContext context)
    {
        lock (_gate)
        {
            if (_armed is not { } armed || !context.ChangeTracker.Entries<IdentityUserToken<Guid>>().Any(entry =>
                    entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                    && entry.Entity.UserId == armed.UserId
                    && string.Equals(entry.Entity.LoginProvider, PasswordResetCodes.LoginProvider, StringComparison.Ordinal)
                    && string.Equals(entry.Entity.Name, armed.Token, StringComparison.Ordinal)))
                return null;

            _armed = null;
            return armed.UserId;
        }
    }
}
