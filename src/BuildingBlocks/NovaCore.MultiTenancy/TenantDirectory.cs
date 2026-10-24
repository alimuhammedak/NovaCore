using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Authorization;
using NovaCore.EntityFrameworkCore.MultiTenancy;

namespace NovaCore.MultiTenancy;

/// <summary>
/// The tenants, their members and their life cycle (active, suspended, closed, deleted): what a product's own endpoints
/// build on (who may open a tenant, how members are invited, how long a closed tenant's data is kept, are the
/// product's). A change of a tenant's access takes effect on the next request on this server: it drops
/// the cached decisions (<see cref="PermissionCacheExtensions"/>); another server's memory keeps its own until
/// <c>Cache:PermissionExpiration</c> passes. Resolve it from DI (scoped; <c>AddCoreMultiTenancy</c>).
/// </summary>
public sealed class TenantDirectory
{
    private readonly MultiTenancyDbContext store;
    private readonly TenantData tenantData;
    private readonly HybridCache cache;
    private readonly IOptions<MultiTenancyOptions> options;
    private readonly TimeProvider timeProvider;

    // Internal, as the options are: DI builds it through a factory (AddCoreMultiTenancy).
    internal TenantDirectory(
        MultiTenancyDbContext store,
        TenantData tenantData,
        HybridCache cache,
        IOptions<MultiTenancyOptions> options,
        TimeProvider timeProvider)
    {
        this.store = store;
        this.tenantData = tenantData;
        this.cache = cache;
        this.options = options;
        this.timeProvider = timeProvider;
    }

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

    /// <summary>
    /// Makes <paramref name="subject"/> a member of the tenant with <paramref name="roles"/>, or sets the roles of a
    /// member. The member's next request works with them.
    /// </summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="subject">The member's subject (the access token's <c>sub</c>).</param>
    /// <param name="roles">The roles the member holds in the tenant, each one of <c>MultiTenancy:Roles</c>.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>Success, <c>tenant.not_found</c>, or <c>tenant.unknown_role</c>.</returns>
    public async Task<Result> AddMemberAsync(
        Guid tenantId, string subject, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(subject.Length, TenantMembership.MaxSubjectLength, nameof(subject));
        ArgumentNullException.ThrowIfNull(roles);

        if (roles.FirstOrDefault(role => !options.Value.Roles.ContainsKey(role)) is { } unknown)
            return Result.Failure(Error.Validation("tenant.unknown_role", $"'{unknown}' is not a role of MultiTenancy:Roles."));

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

    /// <summary>Ends <paramref name="subject"/>'s membership of the tenant, effective on their next request.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="subject">The member's subject.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>Success, or <c>tenant.member_not_found</c>.</returns>
    public async Task<Result> RemoveMemberAsync(Guid tenantId, string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        var removed = await store.Memberships
            .Where(membership => membership.TenantId == tenantId && membership.Subject == subject)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        if (removed == 0)
            return Result.Failure(Error.NotFound(
                "tenant.member_not_found", $"'{subject}' is not a member of tenant '{tenantId}'."));

        await cache.RemovePermissionsAsync(subject, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// The active tenants <paramref name="subject"/> is a member of, with the roles held in each, by name: what a tenant
    /// picker shows a signed-in caller.
    /// </summary>
    /// <param name="subject">The caller's subject (the access token's <c>sub</c>).</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The memberships, ordered by the tenant's name.</returns>
    public async Task<IReadOnlyList<TenantMembershipSummary>> TenantsOfAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        // Ordered before the record is made: EF translates a member of an anonymous type, not of a record's constructor.
        return await store.Memberships
            .Where(membership => membership.Subject == subject)
            .Join(
                store.Tenants.Where(tenant => tenant.Status == TenantStatus.Active),
                membership => membership.TenantId,
                tenant => tenant.Id,
                (membership, tenant) => new { tenant.Id, tenant.Name, membership.Roles })
            .OrderBy(joined => joined.Name)
            .Select(joined => new TenantMembershipSummary(joined.Id, joined.Name, joined.Roles))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Suspends an active tenant: it is no longer resolved for a request, so its members lose all access to it, until
    /// it is reactivated (<see cref="ActivateAsync"/>). A suspended tenant stays suspended; a closed one stays closed.
    /// </summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>Success, <c>tenant.not_found</c>, or <c>tenant.closed</c>.</returns>
    public async Task<Result> SuspendAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await store.Tenants.FindAsync([tenantId], cancellationToken).ConfigureAwait(false);
        if (tenant is null)
            return NotFound(tenantId);

        // Closing is the later stage: a closed tenant's retention runs from its closing, which a suspension would hide.
        if (tenant.Status == TenantStatus.Closed)
            return Result.Failure(Error.Conflict("tenant.closed", $"Tenant '{tenantId}' is closed; reopen it first."));

        return await ChangeStatusAsync(tenant, TenantStatus.Suspended, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Closes (offboards) a tenant, active or suspended: its members lose all access to it, as with a suspension, and its
    /// data is kept until <see cref="DeleteAsync"/> deletes it, after a retention the product chooses
    /// (<see cref="ClosedBeforeAsync"/>). Until then it can be reopened (<see cref="ActivateAsync"/>).
    /// </summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>Success, or <c>tenant.not_found</c>.</returns>
    public async Task<Result> CloseAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await store.Tenants.FindAsync([tenantId], cancellationToken).ConfigureAwait(false);
        return tenant is null
            ? NotFound(tenantId)
            : await ChangeStatusAsync(tenant, TenantStatus.Closed, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Makes a suspended or closed tenant active again: its members work in it from their next request on this server,
    /// with its data as it was. An active tenant stays active.
    /// </summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <returns>Success, or <c>tenant.not_found</c>.</returns>
    public async Task<Result> ActivateAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await store.Tenants.FindAsync([tenantId], cancellationToken).ConfigureAwait(false);
        return tenant is null
            ? NotFound(tenantId)
            : await ChangeStatusAsync(tenant, TenantStatus.Active, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The tenants closed before <paramref name="cutoff"/>, the first closed first: the ones whose retention has passed
    /// when it is the time of the call minus the retention. NovaCore schedules no deletion; the product runs it.
    /// </summary>
    /// <param name="cutoff">The latest closing time to include, exclusive.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The tenants' ids.</returns>
    public async Task<IReadOnlyList<Guid>> ClosedBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) =>
        await store.Tenants
            .Where(tenant => tenant.Status == TenantStatus.Closed && tenant.StatusChangedOnUtc < cutoff)
            .OrderBy(tenant => tenant.StatusChangedOnUtc)
            .Select(tenant => tenant.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <summary>
    /// Deletes a closed tenant: its rows in every module context and the outbox messages its scopes saved
    /// (<see cref="TenantData"/>), then its memberships and the tenant itself. It can't be reopened afterwards. A call
    /// that failed half way leaves the tenant closed with the rest of its data; repeat it.
    /// </summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="cancellationToken">Cancels the deletion.</param>
    /// <returns>Success, <c>tenant.not_found</c>, or <c>tenant.not_closed</c>.</returns>
    public async Task<Result> DeleteAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await store.Tenants.FindAsync([tenantId], cancellationToken).ConfigureAwait(false);
        if (tenant is null)
            return NotFound(tenantId);

        if (tenant.Status != TenantStatus.Closed)
            return Result.Failure(Error.Conflict(
                "tenant.not_closed", $"Tenant '{tenantId}' is {tenant.Status}; only a closed tenant is deleted."));

        await tenantData.DeleteAsync(tenantId, cancellationToken).ConfigureAwait(false);

        // The tenant last, its memberships with it (cascade): until then a failed deletion can be repeated.
        store.Tenants.Remove(tenant);
        await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await cache.RemoveTenantPermissionsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<Result> ChangeStatusAsync(Tenant tenant, TenantStatus status, CancellationToken cancellationToken)
    {
        if (tenant.Status != status)
        {
            tenant.Status = status;
            tenant.StatusChangedOnUtc = timeProvider.GetUtcNow();
            await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await cache.RemoveTenantPermissionsAsync(tenant.Id, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static Result NotFound(Guid tenantId) =>
        Result.Failure(Error.NotFound("tenant.not_found", $"Tenant '{tenantId}' was not found."));
}

/// <summary>A caller's membership of an active tenant.</summary>
/// <param name="TenantId">The tenant's id, which the caller's requests name.</param>
/// <param name="Name">The tenant's name.</param>
/// <param name="Roles">The roles the caller holds there.</param>
public sealed record TenantMembershipSummary(Guid TenantId, string Name, IReadOnlyList<string> Roles);
