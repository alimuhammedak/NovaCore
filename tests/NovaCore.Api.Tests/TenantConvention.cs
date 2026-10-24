using Microsoft.EntityFrameworkCore.Metadata;
using NovaCore.EntityFrameworkCore.MultiTenancy;

namespace NovaCore.Api.Tests;

/// <summary>
/// The convention test of tenant isolation (docs/plan/PLAN.md F7b.3): an entity a model maps with a <c>TenantId</c>
/// column holds a tenant's rows, so the tenant filter (<see cref="TenantQueryFilter"/>) must be on it, through
/// <c>ITenantEntity</c>. A column of that name without the interface would go unfiltered. NovaCore's own bookkeeping
/// tables (the outbox, the tenancy store) aren't a tenant's data and are left out.
/// </summary>
internal static class TenantConvention
{
    private static readonly string[] BookkeepingAssemblies = ["NovaCore.EntityFrameworkCore", "NovaCore.MultiTenancy"];

    /// <summary>The entity types of <paramref name="model"/> with a <c>TenantId</c> column and no tenant filter.</summary>
    public static IReadOnlyList<string> UnfilteredTenantEntities(IModel model) =>
        model.GetEntityTypes()
            .Where(entityType => entityType.FindProperty("TenantId") is not null
                                 && !BookkeepingAssemblies.Contains(entityType.ClrType.Assembly.GetName().Name, StringComparer.Ordinal)
                                 && FilteredWith(entityType) is null)
            .Select(entityType => entityType.DisplayName())
            .ToList();

    // EF Core filters an owned type with its owner and a derived type with its root.
    private static IQueryFilter? FilteredWith(IEntityType entityType) =>
        entityType.FindOwnership() is { } ownership
            ? FilteredWith(ownership.PrincipalEntityType)
            : entityType.GetRootType().FindDeclaredQueryFilter(TenantQueryFilter.Name);
}
