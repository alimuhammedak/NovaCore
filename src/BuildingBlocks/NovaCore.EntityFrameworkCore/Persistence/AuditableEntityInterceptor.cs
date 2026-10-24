using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NovaCore.Abstractions.Entities;
using NovaCore.Abstractions.Identity;

namespace NovaCore.EntityFrameworkCore.Persistence;

/// <summary>
/// Stamps <see cref="IAuditableEntity"/> rows with who/when on every save, in one place, so no
/// handler ever has to remember to set CreatedBy/ModifiedAt. Wire it into each module's DbContext.
/// The time comes from the injected <see cref="TimeProvider"/>. "Who" is the caller's subject
/// (<see cref="ICurrentUser.UserId"/>, the token's <c>sub</c>), or <c>system</c> without a caller: the subject never
/// changes, and it is a pseudonymous id rather than a user name, which can be an email address.
/// </summary>
public sealed class AuditableEntityInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
            return;

        var now = timeProvider.GetUtcNow();
        var user = currentUser.UserId ?? "system";

        foreach (EntityEntry<IAuditableEntity> entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.CreatedBy = user;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAtUtc = now;
                    entry.Entity.ModifiedBy = user;
                    break;
            }
        }
    }
}
