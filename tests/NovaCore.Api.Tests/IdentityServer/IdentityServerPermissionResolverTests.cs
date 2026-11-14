using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The add-on's <see cref="IPermissionResolver"/>: a user's permissions are the <c>permission</c> claims of every role
/// they hold plus their own direct grants, read from the store by the token's <c>sub</c>. A member of the Administrator
/// role holds every declared permission (F4c.2), once two-factor authentication is on (F6b.1d).
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class IdentityServerPermissionResolverTests(IdentityServerFixture fixture) : IClassFixture<IdentityServerFixture>
{
    private readonly IdentityServerTestHost _host = fixture.Host;

    [Fact]
    public async Task Role_permissions_and_direct_grants_are_combined()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var role = new ApplicationRole("Reader");
        Assert.True((await roles.CreateAsync(role)).Succeeded);
        await roles.AddClaimAsync(role, new Claim(NovaClaims.Permission, "catalog:products:read"));
        await roles.AddClaimAsync(role, new Claim("other", "ignored"));

        var user = new ApplicationUser { UserName = "reader@novacore.test", Email = "reader@novacore.test", CreatedAtUtc = TimeProvider.System.GetUtcNow() };
        Assert.True((await users.CreateAsync(user)).Succeeded);
        await users.AddToRoleAsync(user, role.Name!);
        await users.AddClaimAsync(user, new Claim(NovaClaims.Permission, "payments:payments:read"));

        var permissions = await ResolveAsync(scope.ServiceProvider, user.Id.ToString());

        Assert.Equal(["catalog:products:read", "payments:payments:read"], permissions.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    [Fact]
    public async Task An_administrator_holds_every_declared_permission_whether_or_not_the_role_carries_it()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // The seed gave the role every permission declared at the time; one declared later isn't on it.
        var administrator = (await roles.FindByNameAsync("Administrator"))!;
        await roles.RemoveClaimAsync(administrator, new Claim(NovaClaims.Permission, "identity:roles:manage"));

        var user = new ApplicationUser { UserName = "admin-resolve@novacore.test", Email = "admin-resolve@novacore.test", CreatedAtUtc = TimeProvider.System.GetUtcNow() };
        Assert.True((await users.CreateAsync(user)).Succeeded);
        await users.AddToRoleAsync(user, "Administrator");
        await users.SetTwoFactorEnabledAsync(user, true);

        var declared = scope.ServiceProvider.GetServices<IPermissionDefinitionProvider>()
            .SelectMany(provider => provider.GetPermissions())
            .Select(permission => permission.Name);

        Assert.Equal(declared.Order(StringComparer.Ordinal), (await ResolveAsync(scope.ServiceProvider, user.Id.ToString())).Order(StringComparer.Ordinal));
    }

    // F6b.1d: the role that holds every permission needs the second factor. Other roles and direct grants don't.
    [Fact]
    public async Task An_administrator_without_two_factor_authentication_gets_nothing_from_the_role()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = "admin-no-2fa@novacore.test", Email = "admin-no-2fa@novacore.test", CreatedAtUtc = TimeProvider.System.GetUtcNow() };
        Assert.True((await users.CreateAsync(user)).Succeeded);
        await users.AddToRoleAsync(user, "Administrator");
        await users.AddClaimAsync(user, new Claim(NovaClaims.Permission, "payments:payments:read"));

        Assert.Equal(["payments:payments:read"], await ResolveAsync(scope.ServiceProvider, user.Id.ToString()));
    }

    // F9.2g: the resolver used to ask for each role and then its claims, two queries per role (N+1).
    [Fact]
    public async Task The_number_of_queries_does_not_grow_with_the_number_of_roles()
    {
        var oneRole = await CreateUserWithRolesAsync("query-count-1@novacore.test", 1);
        var fiveRoles = await CreateUserWithRolesAsync("query-count-5@novacore.test", 5);

        var (oneRoleQueries, oneRolePermissions) = await ResolveCountingQueriesAsync(oneRole);
        var (fiveRolesQueries, fiveRolesPermissions) = await ResolveCountingQueriesAsync(fiveRoles);

        Assert.Single(oneRolePermissions);
        Assert.Equal(5, fiveRolesPermissions.Count);
        // The permissions come from the store, so a count of 0 means the counter saw nothing and the equality proves nothing.
        Assert.True(oneRoleQueries > 0, "The counter saw none of the resolver's queries.");
        Assert.Equal(oneRoleQueries, fiveRolesQueries);
    }

    private async Task<Guid> CreateUserWithRolesAsync(string email, int roleCount)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = email, Email = email, CreatedAtUtc = TimeProvider.System.GetUtcNow() };
        Assert.True((await users.CreateAsync(user)).Succeeded);

        for (var index = 0; index < roleCount; index++)
        {
            var role = new ApplicationRole($"{email}-role-{index}");
            Assert.True((await roles.CreateAsync(role)).Succeeded);
            await roles.AddClaimAsync(role, new Claim(NovaClaims.Permission, $"{email}:permission:{index}"));
            Assert.True((await users.AddToRoleAsync(user, role.Name!)).Succeeded);
        }

        return user.Id;
    }

    // A fresh scope, so nothing the setup tracked answers without a query. The listener sees every host's commands;
    // only those of this scope's context count.
    private async Task<(int Queries, IReadOnlyCollection<string> Permissions)> ResolveCountingQueriesAsync(Guid userId)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityServerDbContext>();
        var counter = new CommandCounter(context);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(counter);

        var permissions = await ResolveAsync(scope.ServiceProvider, userId.ToString());

        return (counter.Count, permissions);
    }

    private sealed class CommandCounter(DbContext context) : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
    {
        private readonly List<IDisposable> _subscriptions = [];
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void OnNext(DiagnosticListener value)
        {
            if (string.Equals(value.Name, DbLoggerCategory.Name, StringComparison.Ordinal))
                lock (_subscriptions)
                    _subscriptions.Add(value.Subscribe(this));
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (string.Equals(value.Key, RelationalEventId.CommandExecuting.Name, StringComparison.Ordinal) && value.Value is CommandEventData data && ReferenceEquals(data.Context, context))
                Interlocked.Increment(ref _count);
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("0199a3c5-0000-7000-8000-000000000000")]
    public async Task An_unknown_subject_holds_no_permissions(string subject)
    {
        await using var scope = _host.Services.CreateAsyncScope();

        Assert.Empty(await ResolveAsync(scope.ServiceProvider, subject));
    }

    private static Task<IReadOnlyCollection<string>> ResolveAsync(IServiceProvider services, string subject) =>
        services.GetRequiredService<IPermissionResolver>().GetPermissionsAsync(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(NovaClaims.Subject, subject)], "test")),
            CancellationToken.None);
}
