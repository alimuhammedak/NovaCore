using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
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

        Assert.Equal(["catalog:products:read", "payments:payments:read"], permissions.Order(StringComparer.Ordinal));
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
