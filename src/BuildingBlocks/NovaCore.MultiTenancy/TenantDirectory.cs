using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Authorization;

namespace NovaCore.MultiTenancy;

/// <summary>
/// The tenants and their members: what a product's own endpoints build on (who may open a tenant, how members are
/// invited, are the product's). A change that narrows access takes effect on the next request on this server: it drops
/// the cached decisions (<see cref="PermissionCacheExtensions"/>); another server's memory keeps its own until
/// <c>Cache:PermissionExpiration</c> passes.
/// </summary>
/// <param name="store">The tenancy store.</param>
/// <param name="cache">The cache of the core's access decisions.</param>
/// <param name="timeProvider">The clock status changes are stamped with.</param>
public sealed class TenantDirectory(MultiTenancyDbContext store, HybridCache cache, TimeProvider timeProvider)
{
    /// <summary>Opens a tenant, active, with no members.</summary>
    /// <param name="name">The tenant's name, at most 200 characters.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>The new tenant's id.</returns>
    public async Task<Guid> CreateAsync(string name, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, Tenant.MaxNameLength, nameof(name));

        var tenant = new Tenant
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Status = TenantStatus.Active,
            StatusChangedOnUtc = timeProvider.GetUtcNow()
        };
        store.Tenants.Add(tenant);
        await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return tenant.Id;
    }

    /// <summary>Makes <paramref name="subject"/> a member of the tenant with <paramref name="roles"/>, or sets the roles of a member.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="subject">The member's subject (the access token's <c>sub</c>).</param>
    /// <param name="roles">The roles the member holds in the tenant.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>Success, or <c>tenant.not_found</c>.</returns>
    public async Task<Result> AddMemberAsync(
        Guid tenantId, string subject, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(subject.Length, TenantMembership.MaxSubjectLength, nameof(subject));
        ArgumentNullException.ThrowIfNull(roles);

        if (!await store.Tenants.AnyAsync(tenant => tenant.Id == tenantId, cancellationToken).ConfigureAwait(false))
            return NotFound(tenantId);

        var membership = await store.Memberships.FindAsync([tenantId, subject], cancellationToken).ConfigureAwait(false);
        if (membership is null)
            store.Memberships.Add(new TenantMembership { TenantId = tenantId, Subject = subject, Roles = [.. roles] });
        else
            membership.Roles = [.. roles];
        await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cache.RemovePermissionsAsync(subject, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// Suspends an active tenant: it is no longer resolved for a request, so its members lose all access to it, until
    /// it is reactivated. A suspended tenant stays suspended.
    /// </summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>Success, or <c>tenant.not_found</c>.</returns>
    public Task<Result> SuspendAsync(Guid tenantId, CancellationToken cancellationToken) =>
        ChangeStatusAsync(tenantId, TenantStatus.Suspended, cancellationToken);

    private async Task<Result> ChangeStatusAsync(Guid tenantId, TenantStatus status, CancellationToken cancellationToken)
    {
        var tenant = await store.Tenants.FindAsync([tenantId], cancellationToken).ConfigureAwait(false);
        if (tenant is null)
            return NotFound(tenantId);

        if (tenant.Status != status)
        {
            tenant.Status = status;
            tenant.StatusChangedOnUtc = timeProvider.GetUtcNow();
            await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await cache.RemoveTenantPermissionsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static Result NotFound(Guid tenantId) =>
        Result.Failure(Error.NotFound("tenant.not_found", $"Tenant '{tenantId}' was not found."));
}
