using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NovaCore.Abstractions.MultiTenancy;

namespace NovaCore.EntityFrameworkCore.MultiTenancy;

/// <summary>
/// Keeps a save inside the scope's tenant (<see cref="TenantContext"/>), for every <see cref="ITenantEntity"/> it saves:
/// a new row without a tenant gets the scope's, and the save fails when it would add a row of another tenant, change or
/// delete one, move a row to another tenant, or write a tenant row without a tenant. <c>AddModuleDbContext</c> adds it to
/// every module context. It sees what <c>SaveChanges</c> saves; <c>ExecuteUpdate</c>/<c>ExecuteDelete</c> reach only the
/// scope's rows through the query filter, and raw SQL is the caller's own.
/// </summary>
internal sealed class TenantWriteGuard : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Guard(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Guard(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Guard(DbContext? context)
    {
        if (context is null)
            return;

        // Entries runs DetectChanges, so a tenant changed on a tracked entity shows as modified.
        var entries = context.ChangeTracker.Entries<ITenantEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        if (entries.Count == 0)
            return;

        var tenant = TenantQueryFilter.TenantOf(context)
            ?? throw new InvalidOperationException(
                $"Saving {Describe(entries[0])} needs a tenant, and this scope has none: a request gets its tenant once "
                + $"the caller is validated for it; other work gives its scope one ({nameof(TenantContext)}.{nameof(TenantContext.Use)}).");

        foreach (var entry in entries)
        {
            var tenantId = entry.Property(entity => entity.TenantId);
            switch (entry.State)
            {
                case EntityState.Added when entry.Entity.TenantId == Guid.Empty:
                    entry.Entity.TenantId = tenant;
                    break;
                case EntityState.Added when entry.Entity.TenantId != tenant:
                    throw Refused(entry, "add a row of another tenant");
                case EntityState.Modified when tenantId.IsModified && tenantId.CurrentValue != tenantId.OriginalValue:
                    throw Refused(entry, "move a row to another tenant");
                case EntityState.Modified or EntityState.Deleted when tenantId.OriginalValue != tenant:
                    throw Refused(entry, entry.State == EntityState.Deleted ? "delete a row of another tenant" : "change a row of another tenant");
            }
        }
    }

    private static InvalidOperationException Refused(EntityEntry<ITenantEntity> entry, string what) =>
        new($"Saving {Describe(entry)} would {what}: a scope writes only its own tenant's rows.");

    private static string Describe(EntityEntry<ITenantEntity> entry) => entry.Metadata.DisplayName();
}
