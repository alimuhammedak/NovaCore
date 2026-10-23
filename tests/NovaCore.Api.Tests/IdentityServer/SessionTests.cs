using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The signed-in caller's session endpoints (DECISIONS D-027): <c>/me</c> shows the server-resolved permissions;
/// "sign out everywhere" and a password change revoke every refresh token and end the provider's sign-in session.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class SessionTests(IdentityServerFixture fixture) : IClassFixture<IdentityServerFixture>
{
    private const string Password = "a long passphrase for tests";
    private const string NewPassword = "another long passphrase";

    private readonly IdentityServerTestHost _host = fixture.Host;

    [Fact]
    public async Task Me_shows_the_caller_with_the_permissions_resolved_on_the_server()
    {
        var user = await _host.CreateUserAsync("me@novacore.test", Password, IdentityServerTestHost.ProbePermission);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("me@novacore.test", Password);

        var me = await (await flow.CallApiAsync("/api/v1/auth/me", tokens.AccessToken)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(user.Id.ToString(), me.GetProperty("userId").GetString());
        Assert.Equal("me@novacore.test", me.GetProperty("email").GetString());
        Assert.Equal([IdentityServerTestHost.ProbePermission], me.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
    }

    [Fact]
    public async Task Me_needs_a_signed_in_caller()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _host.CreateClient().GetAsync(new Uri("/api/v1/auth/me", UriKind.Relative))).StatusCode);
    }

    [Fact]
    public async Task Signing_out_everywhere_ends_every_session()
    {
        await _host.CreateUserAsync("everywhere@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("everywhere@novacore.test", Password);

        var logoutAll = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/logout-all", tokens.AccessToken, body: null);

        Assert.Equal(HttpStatusCode.NoContent, logoutAll.StatusCode);
        await AssertSessionsEndedAsync(flow, tokens);
    }

    [Fact]
    public async Task Changing_the_password_ends_every_session()
    {
        await _host.CreateUserAsync("change@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("change@novacore.test", Password);

        var change = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/change-password", tokens.AccessToken,
            new { currentPassword = Password, newPassword = NewPassword });

        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        await AssertSessionsEndedAsync(flow, tokens);

        using var fresh = new OidcFlow(_host);
        Assert.False(string.IsNullOrEmpty((await fresh.SignInAsync("change@novacore.test", NewPassword)).AccessToken));
    }

    [Fact]
    public async Task A_wrong_current_password_changes_nothing()
    {
        await _host.CreateUserAsync("keep@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("keep@novacore.test", Password);

        var change = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/change-password", tokens.AccessToken,
            new { currentPassword = "not the current one", newPassword = NewPassword });

        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await flow.RefreshAsync(tokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Wrong_current_passwords_lock_the_account()
    {
        var user = await _host.CreateUserAsync("change-lockout@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("change-lockout@novacore.test", Password);

        // A stolen access token must not allow guessing the password more often than the sign-in page does.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var wrong = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/change-password", tokens.AccessToken,
                new { currentPassword = $"wrong guess number {attempt}", newPassword = NewPassword });
            Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        }

        // Locked: even the right password changes nothing now.
        var right = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/change-password", tokens.AccessToken,
            new { currentPassword = Password, newPassword = NewPassword });
        Assert.Equal(HttpStatusCode.BadRequest, right.StatusCode);

        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var stored = (await users.FindByIdAsync(user.Id.ToString()))!;
        Assert.True(await users.IsLockedOutAsync(stored));
        Assert.True(await users.CheckPasswordAsync(stored, Password));
    }

    [Fact]
    public async Task A_password_change_clears_the_failed_attempts()
    {
        var user = await _host.CreateUserAsync("change-count@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("change-count@novacore.test", Password);

        await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/change-password", tokens.AccessToken,
            new { currentPassword = "not the current one", newPassword = NewPassword });
        Assert.Equal(1, await FailedAttemptsAsync(user.Id));

        var change = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/change-password", tokens.AccessToken,
            new { currentPassword = Password, newPassword = NewPassword });

        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        Assert.Equal(0, await FailedAttemptsAsync(user.Id));
    }

    [Fact]
    public async Task A_short_new_password_is_a_validation_error()
    {
        await _host.CreateUserAsync("short@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("short@novacore.test", Password);

        var change = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/change-password", tokens.AccessToken,
            new { currentPassword = Password, newPassword = "too-short" });

        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
        Assert.True((await change.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("NewPassword", out _));
    }

    private async Task<int> FailedAttemptsAsync(Guid userId)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.GetAccessFailedCountAsync((await users.FindByIdAsync(userId.ToString()))!);
    }

    // The refresh token is revoked, and the browser's provider session no longer skips the sign-in page.
    private static async Task AssertSessionsEndedAsync(OidcFlow flow, TokenSet tokens)
    {
        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(await flow.RefreshAsync(tokens.RefreshToken)));

        var authorize = await flow.Browser.GetAsync(OidcFlow.AuthorizeUri(OidcFlow.NewPkce().Challenge));
        Assert.Equal(HttpStatusCode.Redirect, authorize.StatusCode);
        Assert.StartsWith(IdentityServerTestHost.Authority + "/account/login", authorize.Headers.Location!.ToString(), StringComparison.Ordinal);
    }
}
