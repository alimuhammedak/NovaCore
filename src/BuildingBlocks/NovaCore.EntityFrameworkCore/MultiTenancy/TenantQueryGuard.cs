using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NovaCore.EntityFrameworkCore.MultiTenancy;

/// <summary>
/// Refuses a query that turns every query filter off (<c>IgnoreQueryFilters()</c>) on a context whose model filters
/// tenant entities: that would turn the tenant filter off with the others, by accident. Another filter is turned off by
/// its name, and the tenant filter too when a read across tenants is meant (<see cref="TenantQueryFilter.Name"/>). The
/// check depends on the query alone, never on the scope, so it is safe where EF Core caches the compiled query.
/// </summary>
/// <remarks>
/// A singleton interceptor is registered in EF Core's internal service provider, which EF Core caches by the interceptor
/// instances: a guard per host would build an internal provider per host. It is stateless, so every context shares
/// <see cref="Instance"/>.
/// </remarks>
internal sealed class TenantQueryGuard : IQueryExpressionInterceptor
{
    private TenantQueryGuard()
    {
    }

    public static TenantQueryGuard Instance { get; } = new();

    private static readonly MethodInfo IgnoreEveryFilter = typeof(EntityFrameworkQueryableExtensions).GetMethods()
        .Single(method => string.Equals(method.Name, nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters), StringComparison.Ordinal)
                          && method.GetParameters().Length == 1);

    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
    {
        if (eventData.Context is { } context
            && context.Model.GetEntityTypes().Any(entityType => entityType.FindDeclaredQueryFilter(TenantQueryFilter.Name) is not null)
            && IgnoresEveryFilter.In(queryExpression))
            throw new InvalidOperationException(
                $"IgnoreQueryFilters() would turn the tenant filter off with the others on {context.GetType().Name}. Name "
                + $"the filters to turn off: IgnoreQueryFilters([\"...\"]); a read across tenants names "
                + $"{nameof(TenantQueryFilter)}.{nameof(TenantQueryFilter.Name)}.");

        return queryExpression;
    }

    private sealed class IgnoresEveryFilter : ExpressionVisitor
    {
        private bool _found;

        public static bool In(Expression expression)
        {
            var visitor = new IgnoresEveryFilter();
            visitor.Visit(expression);
            return visitor._found;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsGenericMethod && node.Method.GetGenericMethodDefinition() == IgnoreEveryFilter)
                _found = true;

            return base.VisitMethodCall(node);
        }
    }
}
