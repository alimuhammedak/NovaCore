using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.MultiTenancy;
using NovaCore.EntityFrameworkCore.Messaging;
using NovaCore.EntityFrameworkCore.Persistence;

namespace NovaCore.EntityFrameworkCore.MultiTenancy;

/// <summary>
/// A tenant's rows in every module context (<see cref="PersistenceExtensions.AddModuleDbContext{TContext}"/>): its
/// <see cref="ITenantEntity"/> rows and the outbox messages its scopes saved. Resolve it from DI (a singleton).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Each context is deleted from in a scope of its own that works in the tenant, so the tenant filter keeps every
/// other tenant's rows out of reach, and in one transaction: a context's rows go together or not at all. The contexts don't
/// share a transaction; a call that failed half way deletes the rest when repeated.</item>
/// <item>A dependent goes before its principal (Microsoft: "deleting dependents before a principal"), so a relationship
/// between tenant entities needs no cascade in the database. A row of another type that points at a tenant row blocks the
/// deletion unless its foreign key cascades.</item>
/// <item>A tenant entity mapped to more than one table (TPT) is out of reach of a bulk delete, which acts on one table:
/// the deletion fails.</item>
/// </list>
/// </remarks>
public sealed class TenantData
{
    private static readonly MethodInfo DeleteRows =
        typeof(TenantData).GetMethod(nameof(DeleteRowsAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly IEnumerable<ModuleDbContextDescriptor> contexts;

    // Internal: DI builds it (AddModuleDbContext), with the module contexts the host registered.
    internal TenantData(IServiceScopeFactory scopeFactory, IEnumerable<ModuleDbContextDescriptor> contexts)
    {
        this.scopeFactory = scopeFactory;
        this.contexts = contexts;
    }

    /// <summary>Deletes the tenant's rows from every module context.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="cancellationToken">Cancels the deletion; a context whose transaction didn't commit keeps its rows.</param>
    public async Task DeleteAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);

        foreach (var descriptor in contexts)
        {
            var scope = scopeFactory.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                scope.ServiceProvider.GetRequiredService<TenantContext>().Use(tenantId);
                var context = (DbContext)scope.ServiceProvider.GetRequiredService(descriptor.ContextType);
                await DeleteFromAsync(context, tenantId, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task DeleteFromAsync(DbContext context, Guid tenantId, CancellationToken cancellationToken)
    {
        var tenantTypes = InDeletionOrder(context.Model);
        var hasOutbox = context.Model.FindEntityType(typeof(OutboxMessage)) is not null;
        if (tenantTypes.Count == 0 && !hasOutbox)
            return;

        // A transaction of our own runs inside the execution strategy, so a retrying strategy retries all of it.
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                foreach (var entityType in tenantTypes)
                {
                    await ((Task<int>)DeleteRows.MakeGenericMethod(entityType.ClrType).Invoke(null, [context, cancellationToken])!)
                        .ConfigureAwait(false);
                }

                if (hasOutbox)
                {
                    await context.Set<OutboxMessage>()
                        .Where(message => message.TenantId == tenantId)
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    // The tenant filter narrows the delete to the scope's tenant.
    private static Task<int> DeleteRowsAsync<TEntity>(DbContext context, CancellationToken cancellationToken)
        where TEntity : class =>
        context.Set<TEntity>().ExecuteDeleteAsync(cancellationToken);

    // The root tenant entity types that have a table, each after every one that points at it: a type is deleted once its
    // dependents are. Types that point at each other keep the model's order; the database then decides (a cascade).
    private static List<IEntityType> InDeletionOrder(IModel model)
    {
        var types = model.GetEntityTypes()
            .Where(type => type.BaseType is null
                           && typeof(ITenantEntity).IsAssignableFrom(type.ClrType)
                           && type.GetTableName() is not null)
            .ToList();
        var principalsOf = types.ToDictionary(
            type => type,
            type => type.GetDerivedTypesInclusive()
                .SelectMany(derived => derived.GetDeclaredForeignKeys())
                .Select(foreignKey => foreignKey.PrincipalEntityType.GetRootType())
                .Where(principal => principal != type && types.Contains(principal))
                .ToHashSet());

        var ordered = new List<IEntityType>(types.Count);
        var remaining = types.ToList();
        while (remaining.Count > 0)
        {
            var next = remaining.FirstOrDefault(type =>
                    !remaining.Any(other => other != type && principalsOf[other].Contains(type)))
                ?? remaining[0];
            ordered.Add(next);
            remaining.Remove(next);
        }

        return ordered;
    }
}
