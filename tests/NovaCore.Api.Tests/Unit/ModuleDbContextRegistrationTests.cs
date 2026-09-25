using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Identity;
using NovaCore.EntityFrameworkCore.Persistence;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

public class ModuleDbContextRegistrationTests
{
    [Fact]
    public void A_module_DbContext_gets_the_audit_interceptor_without_AddCorePersistence()
    {
        // Every module adds AuditableEntityInterceptor to its DbContext options. AddModuleDbContext registers it itself,
        // so a host that has not called AddCorePersistence (or calls it later) cannot fail on the first query.
        var services = new ServiceCollection()
            .AddScoped<ICurrentUser, AnonymousUser>()
            .AddModuleDbContext<SampleDbContext>((provider, options) => options
                .UseSqlite("DataSource=:memory:")
                .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>()));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<SampleDbContext>());
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
