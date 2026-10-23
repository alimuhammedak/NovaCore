using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Plays a second request racing the next one that records a used authenticator code. Once armed, the next save that adds
/// or changes a user's <c>[NovaCore]/UsedAuthenticatorCodes</c> token first lets the competing request run to its end: both
/// requests read the user before either writes, on every run, instead of when the scheduler happens to interleave them.
/// </summary>
public sealed class UsedCodeRace : SaveChangesInterceptor
{
    private readonly Lock _gate = new();
    private Func<Task>? _competitor;

    /// <summary>Runs <paramref name="competitor"/> (once) before the next save that records a used code.</summary>
    public void Arm(Func<Task> competitor)
    {
        lock (_gate)
            _competitor = competitor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (Take(eventData.Context!) is { } competitor)
            await competitor();

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // The competitor's own save comes through here too, disarmed by then.
    private Func<Task>? Take(DbContext context)
    {
        lock (_gate)
        {
            if (_competitor is null
                || !context.ChangeTracker.Entries<IdentityUserToken<Guid>>().Any(entry =>
                    entry.State is EntityState.Added or EntityState.Modified
                    && entry.Entity is { LoginProvider: "[NovaCore]", Name: "UsedAuthenticatorCodes" }))
                return null;

            var competitor = _competitor;
            _competitor = null;
            return competitor;
        }
    }
}
