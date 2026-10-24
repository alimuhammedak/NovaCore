using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NovaCore.Abstractions.Identity;
using NovaCore.EntityFrameworkCore.Messaging;
using NovaCore.EntityFrameworkCore.Persistence;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// A module registers its DbContext without a database provider; the host gives every module context its provider once
/// (<c>AddModuleDatabase</c>), and the module's migration history stays in the module's schema (docs/plan/DECISIONS.md
/// D-068).
/// </summary>
public class ModuleDbContextRegistrationTests
{
    private const string Sqlite = "Microsoft.EntityFrameworkCore.Sqlite";

    [Fact]
    public void A_module_DbContext_gets_the_audit_interceptor_without_AddCorePersistence()
    {
        // Every module adds AuditableEntityInterceptor to its DbContext options. AddModuleDbContext registers it itself,
        // so a host that has not called AddCorePersistence (or calls it later) cannot fail on the first query.
        var services = new ServiceCollection()
            .AddScoped<ICurrentUser, AnonymousUser>()
            .AddModuleDatabase((_, options) => options.UseSqlite("DataSource=:memory:"))
            .AddModuleDbContext<SampleDbContext>("sample", (provider, options) => options
                .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>()));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<SampleDbContext>());
    }

    [Fact]
    public void A_module_context_gets_the_hosts_database_and_keeps_its_migration_history_in_its_schema()
    {
        var services = new ServiceCollection()
            .AddScoped<ICurrentUser, AnonymousUser>()
            .AddModuleDatabase((_, options) => options.UseSqlite("DataSource=:memory:"))
            .AddModuleDbContext<SampleDbContext>("sample", (provider, options) => options
                .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>()));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
        var options = context.GetService<IDbContextOptions>();

        Assert.Equal(Sqlite, context.Database.ProviderName);
        var relational = RelationalOptionsExtension.Extract(options);
        Assert.Equal("sample", relational.MigrationsHistoryTableSchema);
        Assert.Null(relational.MigrationsHistoryTableName);
        var interceptors = options.FindExtension<CoreOptionsExtension>()!.Interceptors!;
        Assert.Contains(interceptors, interceptor => interceptor is AuditableEntityInterceptor);
        Assert.Contains(interceptors, interceptor => interceptor is OutboxInterceptor);
    }

    [Fact]
    public void A_later_module_database_replaces_the_earlier_one()
    {
        // A test host points the module contexts at its own database this way: the host's provider is never applied, so
        // the context doesn't end up with two providers.
        var services = new ServiceCollection()
            .AddModuleDatabase((_, options) => options.UseSqlServer("Server=localhost;Database=Unused"))
            .AddModuleDbContext<SampleDbContext>("sample", (_, _) => { })
            .AddModuleDatabase((_, options) => options.UseSqlite("DataSource=:memory:"));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.Equal(Sqlite, scope.ServiceProvider.GetRequiredService<SampleDbContext>().Database.ProviderName);
    }

    [Fact]
    public async Task A_host_without_a_module_database_does_not_start()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddModuleDbContext<SampleDbContext>("sample", (_, _) => { });
        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("AddModuleDatabase", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Many_hosts_share_one_EF_internal_service_provider(bool withMemoryCache)
    {
        // EF Core caches its internal service providers by the options, singleton interceptors included, and fails once a
        // process holds more than twenty (ManyServiceProvidersCreatedWarning). What AddModuleDbContext adds must be the
        // same for every host, or each host would build one of its own. A host's own IMemoryCache (HybridCache registers
        // one) doesn't split them (measured, docs/plan/RESEARCH.md R-130).
        for (var host = 0; host < 25; host++)
        {
            var services = new ServiceCollection()
                .AddModuleDatabase((_, options) => options.UseSqlite("DataSource=:memory:"))
                .AddModuleDbContext<SampleDbContext>("sample", (_, _) => { });
            if (withMemoryCache)
                services.AddMemoryCache();

            using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            using var scope = provider.CreateScope();
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<SampleDbContext>().Model);
        }
    }

    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options);

    private sealed class AnonymousUser : ICurrentUser
    {
        public bool IsAuthenticated => false;
        public string? UserId => null;
        public string? UserName => null;
        public string? Email => null;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }
}
