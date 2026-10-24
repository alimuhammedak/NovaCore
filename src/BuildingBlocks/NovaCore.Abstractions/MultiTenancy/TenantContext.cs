namespace NovaCore.Abstractions.MultiTenancy;

/// <summary>
/// The tenant a unit of work runs in, one per DI scope. The code that owns the scope gives it with <see cref="Use"/>
/// before it touches tenant data. Without one, <see cref="ITenantEntity"/> rows are neither read nor written: the query
/// filter matches none and a save of one is refused.
/// </summary>
/// <remarks>
/// A scope works in one tenant: the tenant is given once and can't change, so nothing read in one tenant is written in
/// another by the same unit of work. Work across tenants runs one scope per tenant; a read across tenants turns the
/// tenant filter off by its name, explicitly (<c>IgnoreQueryFilters([TenantQueryFilter.Name])</c>).
/// </remarks>
public sealed class TenantContext
{
    /// <summary>The tenant of this scope, or null before one is given.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Gives this scope its tenant. Giving the same tenant again changes nothing.</summary>
    /// <param name="tenantId">The tenant's id.</param>
    /// <exception cref="ArgumentException"><paramref name="tenantId"/> is <see cref="Guid.Empty"/>.</exception>
    /// <exception cref="InvalidOperationException">The scope already has another tenant.</exception>
    public void Use(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("A tenant id is not empty.", nameof(tenantId));

        if (TenantId is { } current && current != tenantId)
            throw new InvalidOperationException(
                $"This scope works in tenant {current}; it can't move to tenant {tenantId}. Use a scope per tenant.");

        TenantId = tenantId;
    }
}
