using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.MultiTenancy;

namespace NovaCore.EntityFrameworkCore.MultiTenancy;

/// <summary>
/// The query filter every module context puts on its <see cref="ITenantEntity"/> types (<c>AddModuleDbContext</c>): a
/// query reads only the rows of the scope's tenant (<see cref="TenantContext"/>), and none without one.
/// </summary>
public static class TenantQueryFilter
{
    /// <summary>
    /// The filter's name. A read across tenants turns it off by this name, explicitly:
    /// <c>IgnoreQueryFilters([TenantQueryFilter.Name])</c>. Another filter turned off by its name leaves it on; turning
    /// every filter off at once (<c>IgnoreQueryFilters()</c>) fails on a context with tenant entities, so the tenant filter
    /// is never turned off by accident.
    /// </summary>
    public const string Name = "NovaCore.Tenant";

    private static readonly MethodInfo TenantOfContext =
        typeof(TenantQueryFilter).GetMethod(nameof(TenantOf), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo TenantProperty =
        typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(Guid));

    /// <summary>
    /// <c>entity =&gt; (Guid?)EF.Property&lt;Guid&gt;(entity, "TenantId") == TenantOf(context)</c>, where <c>context</c> is a
    /// node typed <see cref="DbContext"/>: EF Core binds such a node to the context that runs the query and evaluates the
    /// call as a parameter on every execution (docs/plan/RESEARCH.md R-129), so the cached model reads each query's tenant.
    /// Without a tenant the parameter is null, which equals no row's tenant: not even a row whose tenant is the empty Guid,
    /// as a migration that adds the column to a table with rows gives them.
    /// </summary>
    internal static LambdaExpression For(Type entityType)
    {
        var entity = Expression.Parameter(entityType, "entity");
        var context = Expression.Constant(null, typeof(DbContext));

        return Expression.Lambda(
            Expression.Equal(
                Expression.Convert(
                    Expression.Call(TenantProperty, entity, Expression.Constant(nameof(ITenantEntity.TenantId))),
                    typeof(Guid?)),
                Expression.Call(TenantOfContext, context)),
            entity);
    }

    /// <summary>
    /// The tenant of the scope that resolved <paramref name="context"/>, or null: the context's application service
    /// provider is that scope's (<c>AddDbContext</c>), and a context built without DI has none.
    /// </summary>
    internal static Guid? TenantOf(DbContext context) =>
        context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider
            ?.GetService<TenantContext>()?.TenantId;
}
