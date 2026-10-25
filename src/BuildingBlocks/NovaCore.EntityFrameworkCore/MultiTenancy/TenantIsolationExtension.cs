using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.MultiTenancy;

namespace NovaCore.EntityFrameworkCore.MultiTenancy;

/// <summary>
/// The options extension <c>AddModuleDbContext</c> adds to every module context, so the context's model filters its
/// <see cref="ITenantEntity"/> types by tenant without the module asking: it brings
/// <see cref="TenantQueryFilterConvention"/> into the context's conventions. A model customizer would do it too, but the
/// SQLite test contexts replace the customizer (docs/plan/DECISIONS.md D-076 (5)).
/// </summary>
internal sealed class TenantIsolationExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services) =>
        new EntityFrameworkServicesBuilder(services).TryAdd<IConventionSetPlugin, TenantConventionSetPlugin>();

    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "NovaCoreTenantIsolation ";

        // It has no settings: every context with it can share one EF service provider.
        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) =>
            debugInfo["NovaCore:TenantIsolation"] = "1";
    }
}

/// <summary>Adds <see cref="TenantQueryFilterConvention"/> to a context's conventions.</summary>
internal sealed class TenantConventionSetPlugin : IConventionSetPlugin
{
    public ConventionSet ModifyConventions(ConventionSet conventionSet)
    {
        conventionSet.ModelFinalizingConventions.Add(new TenantQueryFilterConvention());
        return conventionSet;
    }
}

/// <summary>
/// Puts the tenant filter (<see cref="TenantQueryFilter"/>) on every <see cref="ITenantEntity"/> type of the model, once
/// the module has configured it. EF Core filters only the root of an inheritance hierarchy and never an owned type, so a
/// tenant entity that is neither a root nor filtered with its root fails the model instead of going unfiltered. The
/// tenant is also a concurrency token, so a write reaches a row only in the tenant the tracked entity started with.
/// </summary>
internal sealed class TenantQueryFilterConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            if (entityType.IsOwned())
                throw new InvalidOperationException(
                    $"{entityType.DisplayName()} is an owned type and a tenant entity: EF Core filters owned types with their "
                    + $"owner only. Put {nameof(ITenantEntity)} on the owner.");

            if (entityType.BaseType is not null)
            {
                if (!typeof(ITenantEntity).IsAssignableFrom(entityType.GetRootType().ClrType))
                    throw new InvalidOperationException(
                        $"{entityType.DisplayName()} is a tenant entity, but the root of its hierarchy, "
                        + $"{entityType.GetRootType().DisplayName()}, isn't: EF Core filters only the root. Put "
                        + $"{nameof(ITenantEntity)} on the root.");
                continue;
            }

            var tenantId = entityType.FindProperty(nameof(ITenantEntity.TenantId))
                ?? throw new InvalidOperationException(
                    $"{entityType.DisplayName()} is a tenant entity without a mapped {nameof(ITenantEntity.TenantId)} "
                    + "property: implement it as a public property.");

            // The tenant joins the WHERE of every UPDATE and DELETE: a detached copy that names another tenant's row by
            // its key and claims this scope's tenant then changes nothing, and the save fails as a concurrency conflict.
            tenantId.Builder.IsConcurrencyToken(true);

            if (entityType.Builder.HasQueryFilter(TenantQueryFilter.Name, TenantQueryFilter.For(entityType.ClrType)) is null)
                throw new InvalidOperationException(
                    $"The tenant filter ('{TenantQueryFilter.Name}') of {entityType.DisplayName()} can't be set: the model "
                    + "configures a filter of that name itself.");
        }
    }
}
