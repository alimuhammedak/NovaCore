using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Time.Testing;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// User and role administration and the seeded roles (PLAN F3b.2): the Administrator role holds every declared
/// permission, the User role the business read permissions; the admin API is guarded by the <c>identity:*</c>
/// permissions; a new registration is a User.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class AdminTests(AdminTests.Fixture fixture) : IClassFixture<AdminTests.Fixture>
{
    public const string AdminEmail = "admin@novacore.test";
    public const string AdminPassword = "the administrator's passphrase";
    private const string Password = "a long passphrase for tests";

    private readonly IdentityServerTestHost _host = fixture.Host;
    private readonly Fixture _fixture = fixture;

    /// <summary>An initial administrator, and a business permission the roles are seeded from.</summary>
    public class Fixture : IdentityServerFixture
    {
        private string? _adminAccessToken;

        /// <summary>The seeded administrator's access token, shared by the class (see <see cref="SignInAdministratorAsync"/>).</summary>
        public async Task<string> AdminAccessTokenAsync() => _adminAccessToken ??= await SignInAdministratorAsync(Host);

        protected override void Configure(WebApplicationBuilder builder)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:Seed:AdminEmail"] = AdminEmail,
                ["Identity:Seed:AdminPassword"] = AdminPassword
            });
            builder.Services.AddPermissionDefinitions<ProbePermissions>();
        }
    }

    /// <summary>
    /// Signs the seeded administrator in with the password, then turns two-factor authentication on in the store: the
    /// role counts only with it (F6b.1d). A later sign-in would ask for an authenticator code, so the access token
    /// (15 minutes) is what the tests use.
    /// </summary>
    public static async Task<string> SignInAdministratorAsync(IdentityServerTestHost host)
    {
        using var flow = new OidcFlow(host);
        var tokens = await flow.SignInAsync(AdminEmail, AdminPassword);

        await using var scope = host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var administrator = (await users.FindByEmailAsync(AdminEmail))!;
        Assert.True((await users.SetTwoFactorEnabledAsync(administrator, true)).Succeeded);

        // The sign-in resolved and cached the permissions without the second factor; the API drops them the same way.
        await host.Services.GetRequiredService<HybridCache>().RemovePermissionsAsync(administrator.Id.ToString());
        return tokens.AccessToken;
    }

    [Fact]
    public async Task The_seeded_roles_hold_the_declared_permissions()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var administrator = await PermissionsOfAsync(roles, "Administrator");
        var user = await PermissionsOfAsync(roles, "User");

        Assert.Equal(
            ["identity:roles:manage", "identity:roles:read", "identity:users:manage", "identity:users:read", IdentityServerTestHost.ProbePermission],
            administrator);
        Assert.Equal([IdentityServerTestHost.ProbePermission], user);
    }

    [Fact]
    public async Task The_administrator_lists_users()
    {
        using var flow = new OidcFlow(_host);

        var response = await flow.CallApiAsync("/api/v1/admin/users", await _fixture.AdminAccessTokenAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("totalCount").GetInt64() >= 1);
    }

    [Fact]
    public async Task A_user_cannot_manage_roles()
    {
        var user = await CreateUserInRoleAsync("plain@novacore.test", "User");
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("plain@novacore.test", Password);

        var response = await flow.CallApiAsync(HttpMethod.Post, $"/api/v1/admin/users/{user.Id}/roles", tokens.AccessToken,
            new { role = "Administrator" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_administrator_assigns_and_removes_roles()
    {
        var user = await _host.CreateUserAsync("promoted@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var adminToken = await _fixture.AdminAccessTokenAsync();
        var path = $"/api/v1/admin/users/{user.Id}/roles";

        Assert.Equal(HttpStatusCode.NoContent,
            (await flow.CallApiAsync(HttpMethod.Post, path, adminToken, new { role = "User" })).StatusCode);
        Assert.True(await IsInRoleAsync(user.Id, "User"));

        Assert.Equal(HttpStatusCode.NotFound,
            (await flow.CallApiAsync(HttpMethod.Post, path, adminToken, new { role = "NoSuchRole" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await flow.CallApiAsync(HttpMethod.Post, path, adminToken, new { role = "" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await flow.CallApiAsync(HttpMethod.Delete, path + "/User", adminToken, body: null)).StatusCode);
        Assert.False(await IsInRoleAsync(user.Id, "User"));
    }

    // identity:users:manage alone would let its holder make themselves an administrator, who holds every permission.
    [Theory]
    [InlineData("Administrator")]
    [InlineData("administrator")]
    public async Task Granting_the_administrator_role_needs_roles_manage(string role)
    {
        var (flow, accessToken) = await SignedInUserManagerAsync();
        using var ownedFlow = flow;
        var target = await _host.CreateUserAsync($"would-be-admin-{Guid.NewGuid():N}@novacore.test", Password);
        var path = $"/api/v1/admin/users/{target.Id}/roles";

        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync(HttpMethod.Post, path, accessToken, new { role })).StatusCode);
        Assert.False(await IsInRoleAsync(target.Id, "Administrator"));

        // Any other role is identity:users:manage's to grant.
        Assert.Equal(HttpStatusCode.NoContent, (await flow.CallApiAsync(HttpMethod.Post, path, accessToken, new { role = "User" })).StatusCode);
    }

    [Theory]
    [InlineData("Administrator")]
    [InlineData("administrator")]
    public async Task Removing_the_administrator_role_needs_roles_manage(string role)
    {
        var (flow, accessToken) = await SignedInUserManagerAsync();
        using var ownedFlow = flow;
        var target = await CreateUserInRoleAsync($"kept-admin-{Guid.NewGuid():N}@novacore.test", "Administrator");

        var response = await flow.CallApiAsync(HttpMethod.Delete, $"/api/v1/admin/users/{target.Id}/roles/{role}", accessToken, body: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await IsInRoleAsync(target.Id, "Administrator"));
    }

    [Fact]
    public async Task The_last_administrator_keeps_the_role()
    {
        // A host of its own: here the seeded administrator is the only one.
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:Seed:AdminEmail"] = AdminEmail,
                ["Identity:Seed:AdminPassword"] = AdminPassword
            }));
        using var flow = new OidcFlow(host);
        var adminToken = await SignInAdministratorAsync(host);
        Guid adminId;
        await using (var scope = host.Services.CreateAsyncScope())
            adminId = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(AdminEmail))!.Id;

        var response = await flow.CallApiAsync(HttpMethod.Delete, $"/api/v1/admin/users/{adminId}/roles/Administrator", adminToken, body: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var check = host.Services.CreateAsyncScope();
        var users = check.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(await users.IsInRoleAsync((await users.FindByIdAsync(adminId.ToString()))!, "Administrator"));
    }

    [Fact]
    public async Task A_role_change_takes_effect_at_once_and_ends_the_users_sessions()
    {
        var user = await _host.CreateUserAsync("role-change-now@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var userTokens = await flow.SignInAsync("role-change-now@novacore.test", Password);
        using var adminFlow = new OidcFlow(_host);
        var adminToken = await _fixture.AdminAccessTokenAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync("/probe", userTokens.AccessToken)).StatusCode);

        // The User role holds the probe permission (seeded from the declared permissions).
        var path = $"/api/v1/admin/users/{user.Id}/roles";
        Assert.Equal(HttpStatusCode.NoContent,
            (await adminFlow.CallApiAsync(HttpMethod.Post, path, adminToken, new { role = "User" })).StatusCode);

        // The same access token, at once: the cached permissions were dropped. The refresh token is revoked.
        Assert.Equal(HttpStatusCode.OK, (await flow.CallApiAsync("/probe", userTokens.AccessToken)).StatusCode);
        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(await flow.RefreshAsync(userTokens.RefreshToken)));

        Assert.Equal(HttpStatusCode.NoContent,
            (await adminFlow.CallApiAsync(HttpMethod.Delete, path + "/User", adminToken, body: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync("/probe", userTokens.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task A_new_registration_is_a_User()
    {
        var register = await _host.CreateClient().PostAsJsonAsync("/api/v1/auth/register",
            new { email = "joined@novacore.test", password = Password });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Equal(["User"], await users.GetRolesAsync((await users.FindByEmailAsync("joined@novacore.test"))!));
    }

    // A signed-in user who manages users (identity:users:manage, a direct grant) but not roles.
    private async Task<(OidcFlow Flow, string AccessToken)> SignedInUserManagerAsync()
    {
        var email = $"users-manager-{Guid.NewGuid():N}@novacore.test";
        await _host.CreateUserAsync(email, Password, "identity:users:manage");
        var flow = new OidcFlow(_host);
        return (flow, (await flow.SignInAsync(email, Password)).AccessToken);
    }

    private async Task<ApplicationUser> CreateUserInRoleAsync(string email, string role)
    {
        var user = await _host.CreateUserAsync(email, Password);
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True((await users.AddToRoleAsync((await users.FindByIdAsync(user.Id.ToString()))!, role)).Succeeded);
        return user;
    }

    private async Task<bool> IsInRoleAsync(Guid userId, string role)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.IsInRoleAsync((await users.FindByIdAsync(userId.ToString()))!, role);
    }

    private static async Task<string[]> PermissionsOfAsync(RoleManager<ApplicationRole> roles, string role) =>
        (await roles.GetClaimsAsync((await roles.FindByNameAsync(role))!))
        .Where(claim => claim.Type == NovaClaims.Permission)
        .Select(claim => claim.Value)
        .Order(StringComparer.Ordinal)
        .ToArray();

    /// <summary>A business permission, as a module would declare it.</summary>
    public sealed class ProbePermissions : IPermissionDefinitionProvider
    {
        public IReadOnlyCollection<PermissionDefinition> GetPermissions() =>
            [new(IdentityServerTestHost.ProbePermission, "Read the probe", "Probe")];
    }
}

/// <summary>
/// A host whose clock the test moves (<see cref="Clock"/>), with the admin fixture's administrator and permission.
/// Tokens issued after the clock moved would not be valid yet for JwtBearer, which reads the real clock (R-058), so a
/// class sign-ins before it moves the clock and doesn't share the host with another class.
/// </summary>
public sealed class FakeClockIdentityServerFixture : AdminTests.Fixture
{
    public FakeTimeProvider Clock { get; } = new(TimeProvider.System.GetUtcNow());

    protected override void Configure(WebApplicationBuilder builder)
    {
        base.Configure(builder);
        builder.Services.AddSingleton<TimeProvider>(Clock);
        // HybridCache's local entries expire by the memory cache's clock, not TimeProvider (R-057).
        builder.Services.Configure<MemoryCacheOptions>(options => options.Clock = new FakeSystemClock(Clock));
    }

    private sealed class FakeSystemClock(TimeProvider clock) : ISystemClock
    {
        public DateTimeOffset UtcNow => clock.GetUtcNow();
    }
}

/// <summary>
/// PLAN F3a.2's acceptance on a token of this provider: a role change made in the store, outside the admin API (which
/// drops the cached permissions at once), reaches a signed-in user within <c>Cache:PermissionExpiration</c>, without
/// waiting (fake clock).
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class IdentityServerRoleChangeTests(FakeClockIdentityServerFixture fixture)
    : IClassFixture<FakeClockIdentityServerFixture>
{
    private const string Password = "a long passphrase for tests";

    // Cache:PermissionExpiration's default; the test host leaves it unset.
    private static readonly TimeSpan PermissionExpiration = TimeSpan.FromMinutes(5);

    private readonly IdentityServerTestHost _host = fixture.Host;

    [Fact]
    public async Task A_role_change_reaches_a_signed_in_user_within_the_cache_lifetime()
    {
        var user = await _host.CreateUserAsync("role-change@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var userToken = (await flow.SignInAsync("role-change@novacore.test", Password)).AccessToken;
        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync("/probe", userToken)).StatusCode);

        // The User role holds the probe permission (seeded from the declared permissions).
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.AddToRoleAsync((await users.FindByIdAsync(user.Id.ToString()))!, "User")).Succeeded);
        }

        // Same token, still inside the cache lifetime: the old permissions stand...
        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync("/probe", userToken)).StatusCode);

        // ...and once it has passed, the new role's permission is resolved.
        fixture.Clock.Advance(PermissionExpiration + TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.OK, (await flow.CallApiAsync("/probe", userToken)).StatusCode);
    }
}

/// <summary>
/// PLAN F4c.2's acceptance: an administrator removed from the role loses every permission within
/// <c>Cache:PermissionExpiration</c>, although the token still names the role until it expires.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class AdministratorRemovalTests(FakeClockIdentityServerFixture fixture)
    : IClassFixture<FakeClockIdentityServerFixture>
{
    private const string Email = "former-admin@novacore.test";
    private const string Password = "a long passphrase for tests";

    // Cache:PermissionExpiration's default; the test host leaves it unset.
    private static readonly TimeSpan PermissionExpiration = TimeSpan.FromMinutes(5);

    private readonly IdentityServerTestHost _host = fixture.Host;

    [Fact]
    public async Task A_removed_administrator_is_forbidden_once_the_cache_lifetime_has_passed()
    {
        var user = await _host.CreateUserAsync(Email, Password);
        using var flow = new OidcFlow(_host);
        var token = (await flow.SignInAsync(Email, Password)).AccessToken;
        // The role counts with two-factor authentication on (F6b.1d); turned on after the password sign-in.
        await ChangeAdministratorAsync(user.Id, (users, account) => users.SetTwoFactorEnabledAsync(account, true));
        await ChangeAdministratorAsync(user.Id, (users, account) => users.AddToRoleAsync(account, "Administrator"));
        await _host.Services.GetRequiredService<HybridCache>().RemovePermissionsAsync(user.Id.ToString());
        Assert.Equal(HttpStatusCode.OK, (await flow.CallApiAsync("/api/v1/admin/users", token)).StatusCode);

        await ChangeAdministratorAsync(user.Id, (users, account) => users.RemoveFromRoleAsync(account, "Administrator"));

        // Same token, which still carries role=Administrator: once the cached permissions expire, nothing is left.
        fixture.Clock.Advance(PermissionExpiration + TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync("/api/v1/admin/users", token)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync("/probe", token)).StatusCode);
    }

    private async Task ChangeAdministratorAsync(
        Guid userId, Func<UserManager<ApplicationUser>, ApplicationUser, Task<IdentityResult>> change)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True((await change(users, (await users.FindByIdAsync(userId.ToString()))!)).Succeeded);
    }
}

/// <summary>A refresh token dies after its lifetime (7 days), the provider's clock deciding.</summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class RefreshTokenLifetimeTests(FakeClockIdentityServerFixture fixture) : IClassFixture<FakeClockIdentityServerFixture>
{
    [Fact]
    public async Task An_expired_refresh_token_is_refused()
    {
        await fixture.Host.CreateUserAsync("expiry@novacore.test", "a long passphrase for tests");
        using var flow = new OidcFlow(fixture.Host);
        var tokens = await flow.SignInAsync("expiry@novacore.test", "a long passphrase for tests");

        fixture.Clock.Advance(TimeSpan.FromDays(7) + TimeSpan.FromSeconds(1));

        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(await flow.RefreshAsync(tokens.RefreshToken)));
    }
}
