namespace NovaCore.Abstractions.MultiTenancy;

/// <summary>
/// An entity that belongs to one tenant. Every module context isolates these rows by tenant on its own
/// (<c>AddModuleDbContext</c>): a query sees only the rows of the scope's tenant (<see cref="TenantContext"/>), a new row
/// gets the scope's tenant, and a save that would write another tenant's row, or move a row to another tenant, fails.
/// </summary>
/// <remarks>
/// Implement <see cref="TenantId"/> as a public property (it is mapped by that name), on the root type of an inheritance
/// hierarchy: query filters apply to the root (EF Core).
/// </remarks>
public interface ITenantEntity
{
    /// <summary>The tenant the entity belongs to. Leave it empty on a new entity: the save fills it in.</summary>
    Guid TenantId { get; set; }
}
