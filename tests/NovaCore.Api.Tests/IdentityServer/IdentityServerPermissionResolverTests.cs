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
/// they hold plus their own direct grants, read from the store by the token's <c>sub</c>.
/// </summary>
public sealed class IdentityServerPermissionResolverTests : IAsyncLifetime
{
    private IdentityServerTestHost _host = null!;

    public async ValueTask InitializeAsync() => _host = await IdentityServerTestHost.StartAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

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
