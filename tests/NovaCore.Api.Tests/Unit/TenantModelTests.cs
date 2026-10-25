using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Entities;
using NovaCore.Abstractions.MultiTenancy;
using NovaCore.Api.Tests.Databases;
using NovaCore.EntityFrameworkCore.MultiTenancy;
using NovaCore.EntityFrameworkCore.Persistence;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// <c>AddModuleDbContext</c> puts the tenant filter on every tenant entity of a module's model, and a tenant entity EF
/// Core can't filter fails the model instead of going unfiltered (docs/plan/PLAN.md F7b.3, DECISIONS D-076 (5)).
/// </summary>
public class TenantModelTests
{
    [Fact]
    public void A_tenant_entity_gets_the_tenant_filter_and_keeps_its_own()
    {
        var model = ModelOf<NotesDbContext>();

        var note = model.FindEntityType(typeof(Note))!;
        Assert.NotNull(note.FindDeclaredQueryFilter(TenantQueryFilter.Name));
        Assert.NotNull(note.FindDeclaredQueryFilter(NotesDbContext.Unarchived));
    }

    [Fact]
    public void A_context_without_tenant_entities_has_no_tenant_filter()
    {
        var model = ModelOf<PlainContext>();

        Assert.All(model.GetEntityTypes(), entityType => Assert.Empty(entityType.GetDeclaredQueryFilters()));
    }

    [Fact]
    public void A_derived_tenant_entity_under_a_root_that_is_not_one_fails_the_model()
    {
        var error = Assert.Throws<InvalidOperationException>(() => ModelOf<DerivedTenantContext>());

        Assert.Contains("root", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_owned_tenant_entity_fails_the_model()
    {
        var error = Assert.Throws<InvalidOperationException>(() => ModelOf<OwnedTenantContext>());

        Assert.Contains("owned", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unnamed_filter_on_a_tenant_entity_fails_the_model()
    {
        // EF Core 10 doesn't mix a filter without a name with named ones on an entity type; the module names its own.
        Assert.ThrowsAny<InvalidOperationException>(() => ModelOf<UnnamedFilterContext>());
    }

    [Fact]
    public void A_tenant_entity_whose_tenant_is_configured_as_no_concurrency_token_fails_the_model()
    {
        // The token keeps the tenant in every UPDATE and DELETE; a module turning it off would let a detached copy reach
        // another tenant's row by its key.
        var error = Assert.Throws<InvalidOperationException>(() => ModelOf<NoTenantTokenContext>());

        Assert.Contains("concurrency token", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_tenant_entitys_tenant_is_a_concurrency_token()
    {
        var note = ModelOf<NotesDbContext>().FindEntityType(typeof(Note))!;

        Assert.True(note.FindProperty(nameof(Note.TenantId))!.IsConcurrencyToken);
    }

    [Fact]
    public void The_convention_test_catches_a_tenant_column_without_the_tenant_filter()
    {
        Assert.Equal(["Orphan"], TenantConvention.UnfilteredTenantEntities(ModelOf<OrphanContext>()));
        Assert.Empty(TenantConvention.UnfilteredTenantEntities(ModelOf<NotesDbContext>()));
    }

    [Fact]
    public async Task Deleting_a_tenant_whose_entity_maps_to_several_tables_fails_with_ef_cores_reason()
    {
        // A bulk delete acts on one table, so EF Core refuses one on a TPT hierarchy (TenantData's remarks).
        await using var provider = ProviderOf<TptTenantContext>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetRequiredService<TenantData>().DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    // The model a module context gets when the module registers it.
    private static Microsoft.EntityFrameworkCore.Metadata.IModel ModelOf<TContext>()
        where TContext : DbContext
    {
        using var provider = ProviderOf<TContext>();
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TContext>().Model;
    }

    private static ServiceProvider ProviderOf<TContext>()
        where TContext : DbContext =>
        new ServiceCollection()
            .AddModuleDatabase((_, options) => options.UseSqlite("DataSource=:memory:"))
            .AddModuleDbContext<TContext>("sample", (_, _) => { })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

    private sealed class PlainContext(DbContextOptions<PlainContext> options) : DbContext(options)
    {
        public DbSet<Plain> Plains => Set<Plain>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Plain>().Ignore(p => p.DomainEvents).Ignore(p => p.Details);
    }

    private sealed class DerivedTenantContext(DbContextOptions<DerivedTenantContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Plain>().Ignore(p => p.DomainEvents).Ignore(p => p.Details);
            modelBuilder.Entity<TenantPlain>();
        }
    }

    private sealed class OwnedTenantContext(DbContextOptions<OwnedTenantContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Plain>(plain =>
            {
                plain.Ignore(p => p.DomainEvents);
                plain.OwnsOne(p => p.Details);
            });
    }

    private sealed class UnnamedFilterContext(DbContextOptions<UnnamedFilterContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Note>(note =>
            {
                note.Ignore(n => n.DomainEvents);
                note.HasQueryFilter(n => !n.Archived);
            });
    }

    private sealed class TptTenantContext(DbContextOptions<TptTenantContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TenantRoot>().UseTptMappingStrategy();
            modelBuilder.Entity<TenantLeaf>();
        }
    }

    private sealed class NoTenantTokenContext(DbContextOptions<NoTenantTokenContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<Note>(note =>
            {
                note.Ignore(n => n.DomainEvents);
                note.Property(n => n.TenantId).IsConcurrencyToken(false);
            });
    }

    private sealed class OrphanContext(DbContextOptions<OrphanContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Orphan>();
    }

    private class Plain() : Entity<Guid>(Guid.CreateVersion7())
    {
        public Details? Details { get; set; }
    }

    private sealed class TenantPlain : Plain, ITenantEntity
    {
        public Guid TenantId { get; set; }
    }

    private sealed class Details : ITenantEntity
    {
        public Guid TenantId { get; set; }

        public string Text { get; set; } = string.Empty;
    }

    private class TenantRoot : ITenantEntity
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }
    }

    private sealed class TenantLeaf : TenantRoot
    {
        public string Text { get; set; } = string.Empty;
    }

    // A tenant column without ITenantEntity: nothing filters it.
    private sealed class Orphan
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }
    }
}
