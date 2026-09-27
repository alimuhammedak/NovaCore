using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Authenticator-app two-factor authentication (DECISIONS D-027): enabled through the account API, then asked for on the
/// sign-in page after the password. A wrong code counts toward lockout like a wrong password.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class TwoFactorTests(IdentityServerFixture fixture) : IClassFixture<IdentityServerFixture>
{
    private const string Password = "a long passphrase for tests";

    private readonly IdentityServerTestHost _host = fixture.Host;

    [Fact]
    public async Task Once_enabled_sign_in_asks_for_the_authenticator_code()
    {
        await _host.CreateUserAsync("mfa@novacore.test", Password);
        var key = await EnableAsync("mfa@novacore.test");

        using var flow = new OidcFlow(_host);
        var (verifier, afterPassword) = await flow.BeginSignInAsync("mfa@novacore.test", Password);

        Assert.Equal(HttpStatusCode.Redirect, afterPassword.StatusCode);
        Assert.StartsWith("/account/login/2fa", afterPassword.Headers.Location!.OriginalString, StringComparison.Ordinal);

        var afterCode = await flow.SubmitCodeAsync(await flow.Browser.GetAsync(afterPassword.Headers.Location), AuthenticatorCode.For(key));
        var tokens = await flow.FinishSignInAsync(afterCode, verifier);
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
    }

    [Fact]
    public async Task Wrong_codes_lock_the_account()
    {
        var user = await _host.CreateUserAsync("mfa-lockout@novacore.test", Password);
        var key = await EnableAsync("mfa-lockout@novacore.test");

        using var flow = new OidcFlow(_host);
        var (_, afterPassword) = await flow.BeginSignInAsync("mfa-lockout@novacore.test", Password);
        var codePage = await flow.Browser.GetAsync(afterPassword.Headers.Location);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            codePage = await flow.SubmitCodeAsync(codePage, AuthenticatorCode.WrongFor(key));
            Assert.Contains("Invalid authenticator code.", await codePage.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }

        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(await users.IsLockedOutAsync((await users.FindByIdAsync(user.Id.ToString()))!));
    }

    [Fact]
    public async Task A_wrong_code_does_not_enable_two_factor_authentication()
    {
        var user = await _host.CreateUserAsync("mfa-wrong@novacore.test", Password);
        var (flow, accessToken) = await SignedInAsync("mfa-wrong@novacore.test");
        using var ownedFlow = flow;
        var key = await SetUpAsync(flow, accessToken);

        var enable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { code = AuthenticatorCode.WrongFor(key) });

        Assert.Equal(HttpStatusCode.BadRequest, enable.StatusCode);
        Assert.True((await enable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("code", out _));
        Assert.False(await TwoFactorEnabledAsync(user.Id));
    }

    [Fact]
    public async Task A_malformed_code_is_a_validation_error()
    {
        await _host.CreateUserAsync("mfa-short@novacore.test", Password);
        var (flow, accessToken) = await SignedInAsync("mfa-short@novacore.test");
        using var ownedFlow = flow;

        var enable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { code = "12345" });

        Assert.Equal(HttpStatusCode.BadRequest, enable.StatusCode);
        Assert.True((await enable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("Code", out _));
    }

    [Fact]
    public async Task Disabling_removes_the_code_step()
    {
        var user = await _host.CreateUserAsync("mfa-off@novacore.test", Password);
        var (flow, accessToken) = await SignedInAsync("mfa-off@novacore.test");
        using var ownedFlow = flow;
        var key = await SetUpAsync(flow, accessToken);
        await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { code = AuthenticatorCode.For(key) });

        var disable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/disable", accessToken, new { code = AuthenticatorCode.For(key) });

        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);
        Assert.False(await TwoFactorEnabledAsync(user.Id));
        using var again = new OidcFlow(_host);
        Assert.False(string.IsNullOrEmpty((await again.SignInAsync("mfa-off@novacore.test", Password)).AccessToken));
    }

    [Fact]
    public async Task The_code_step_needs_the_password_step_first()
    {
        using var flow = new OidcFlow(_host);

        var response = await flow.Browser.GetAsync("/account/login/2fa?ReturnUrl=%2F");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/account/login?", response.Headers.Location!.OriginalString, StringComparison.Ordinal);
    }

    // Signs in, sets up the authenticator and enables it with the right code; returns the shared key.
    private async Task<string> EnableAsync(string email)
    {
        var (flow, accessToken) = await SignedInAsync(email);
        using var ownedFlow = flow;
        var key = await SetUpAsync(flow, accessToken);

        var enable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { code = AuthenticatorCode.For(key) });
        Assert.Equal(HttpStatusCode.OK, enable.StatusCode);
        Assert.Equal(10, (await enable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("recoveryCodes").GetArrayLength());
        return key;
    }

    private async Task<(OidcFlow Flow, string AccessToken)> SignedInAsync(string email)
    {
        var flow = new OidcFlow(_host);
        return (flow, (await flow.SignInAsync(email, Password)).AccessToken);
    }

    private static async Task<string> SetUpAsync(OidcFlow flow, string accessToken)
    {
        var setup = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/setup", accessToken, body: null);
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        var json = await setup.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("otpauth://totp/", json.GetProperty("authenticatorUri").GetString(), StringComparison.Ordinal);
        return json.GetProperty("sharedKey").GetString()!;
    }

    private async Task<bool> TwoFactorEnabledAsync(Guid userId)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.GetTwoFactorEnabledAsync((await users.FindByIdAsync(userId.ToString()))!);
    }
}
