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
/// sign-in page after the password. A wrong code counts toward lockout like a wrong password. The code step also takes
/// one of the recovery codes, once each (PLAN F4d.3).
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
        var (key, _) = await EnableAsync("mfa@novacore.test");

        using var flow = new OidcFlow(_host);
        var (verifier, afterPassword) = await flow.BeginSignInAsync("mfa@novacore.test", Password);

        Assert.Equal(HttpStatusCode.Redirect, afterPassword.StatusCode);
        Assert.StartsWith("/account/login/2fa", afterPassword.Headers.Location!.OriginalString, StringComparison.Ordinal);

        // The next step's code: the current one enabled two-factor authentication and is used up.
        var afterCode = await flow.SubmitCodeAsync(await flow.Browser.GetAsync(afterPassword.Headers.Location), AuthenticatorCode.For(key, stepsAhead: 1));
        var tokens = await flow.FinishSignInAsync(afterCode, verifier);
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
    }

    [Fact]
    public async Task An_authenticator_code_signs_in_once()
    {
        await _host.CreateUserAsync("mfa-once@novacore.test", Password);
        var (key, _) = await EnableAsync("mfa-once@novacore.test");
        var code = AuthenticatorCode.For(key, stepsAhead: 1);

        using var first = new OidcFlow(_host);
        var (verifier, firstCodePage) = await CodeStepAsync(first, "mfa-once@novacore.test");
        Assert.False(string.IsNullOrEmpty((await first.FinishSignInAsync(await first.SubmitCodeAsync(firstCodePage, code), verifier)).AccessToken));

        // Still inside the authenticator's time window (ASVS 6.5.1).
        using var second = new OidcFlow(_host);
        var (_, secondCodePage) = await CodeStepAsync(second, "mfa-once@novacore.test");
        var replayed = await second.SubmitCodeAsync(secondCodePage, code);

        Assert.Equal(HttpStatusCode.OK, replayed.StatusCode);
        Assert.Contains("Invalid authenticator code.", await replayed.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_code_that_enabled_two_factor_authentication_does_not_sign_in()
    {
        await _host.CreateUserAsync("mfa-enabling-code@novacore.test", Password);
        var (flow, accessToken) = await SignedInAsync("mfa-enabling-code@novacore.test");
        using var ownedFlow = flow;
        var key = await SetUpAsync(flow, accessToken);
        var code = AuthenticatorCode.For(key);
        Assert.Equal(HttpStatusCode.OK,
            (await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { code })).StatusCode);

        using var signIn = new OidcFlow(_host);
        var (_, codePage) = await CodeStepAsync(signIn, "mfa-enabling-code@novacore.test");
        var response = await signIn.SubmitCodeAsync(codePage, code);

        Assert.Contains("Invalid authenticator code.", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_used_code_written_with_a_leading_zero_is_still_used()
    {
        await _host.CreateUserAsync("mfa-zero@novacore.test", Password);
        var (key, _) = await EnableAsync("mfa-zero@novacore.test");
        var code = AuthenticatorCode.For(key, stepsAhead: 1);

        using var first = new OidcFlow(_host);
        var (verifier, firstCodePage) = await CodeStepAsync(first, "mfa-zero@novacore.test");
        Assert.False(string.IsNullOrEmpty((await first.FinishSignInAsync(await first.SubmitCodeAsync(firstCodePage, code), verifier)).AccessToken));

        // Identity compares the codes as numbers, so "0" + code is the same code.
        using var second = new OidcFlow(_host);
        var (_, secondCodePage) = await CodeStepAsync(second, "mfa-zero@novacore.test");
        var replayed = await second.SubmitCodeAsync(secondCodePage, "0" + code);

        Assert.Contains("Invalid authenticator code.", await replayed.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Two_sign_ins_racing_with_one_code_let_one_in()
    {
        await _host.CreateUserAsync("mfa-race@novacore.test", Password);
        var (key, _) = await EnableAsync("mfa-race@novacore.test");
        var code = AuthenticatorCode.For(key, stepsAhead: 1);

        using var first = new OidcFlow(_host);
        using var second = new OidcFlow(_host);
        var (_, firstCodePage) = await CodeStepAsync(first, "mfa-race@novacore.test");
        var (_, secondCodePage) = await CodeStepAsync(second, "mfa-race@novacore.test");

        var responses = await Task.WhenAll(first.SubmitCodeAsync(firstCodePage, code), second.SubmitCodeAsync(secondCodePage, code));

        // The signed-in browser goes back to the authorization endpoint; the other one gets the code form again.
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Redirect);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task Wrong_codes_lock_the_account()
    {
        var user = await _host.CreateUserAsync("mfa-lockout@novacore.test", Password);
        var (key, _) = await EnableAsync("mfa-lockout@novacore.test");

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

        var disable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/disable", accessToken, new { code = AuthenticatorCode.For(key, stepsAhead: 1) });

        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);
        Assert.False(await TwoFactorEnabledAsync(user.Id));
        using var again = new OidcFlow(_host);
        Assert.False(string.IsNullOrEmpty((await again.SignInAsync("mfa-off@novacore.test", Password)).AccessToken));
    }

    [Fact]
    public async Task Setup_keeps_the_key_of_an_enabled_authenticator_secret()
    {
        await _host.CreateUserAsync("mfa-setup@novacore.test", Password);
        var (flow, accessToken) = await SignedInAsync("mfa-setup@novacore.test");
        using var ownedFlow = flow;
        var key = await SetUpAsync(flow, accessToken);
        Assert.Equal(HttpStatusCode.OK,
            (await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { code = AuthenticatorCode.For(key) })).StatusCode);

        // With the key, a stolen access token could compute codes and turn two-factor authentication off.
        var again = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/setup", accessToken, body: null);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.DoesNotContain(key, await again.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_recovery_code_signs_in_once()
    {
        await _host.CreateUserAsync("recovery@novacore.test", Password);
        var (_, recoveryCodes) = await EnableAsync("recovery@novacore.test");

        using var flow = new OidcFlow(_host);
        var (verifier, codePage) = await CodeStepAsync(flow, "recovery@novacore.test");
        // Typed in lower case: the codes are upper case letters and digits.
        var afterCode = await flow.SubmitCodeAsync(codePage, recoveryCodes[0].ToLowerInvariant());
        var tokens = await flow.FinishSignInAsync(afterCode, verifier);
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));

        using var again = new OidcFlow(_host);
        var (_, secondCodePage) = await CodeStepAsync(again, "recovery@novacore.test");
        var reused = await again.SubmitCodeAsync(secondCodePage, recoveryCodes[0]);

        Assert.Equal(HttpStatusCode.OK, reused.StatusCode);
        Assert.Contains("Invalid recovery code.", await reused.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_wrong_recovery_code_counts_as_a_failed_sign_in()
    {
        var user = await _host.CreateUserAsync("recovery-wrong@novacore.test", Password);
        await EnableAsync("recovery-wrong@novacore.test");

        using var flow = new OidcFlow(_host);
        var (_, codePage) = await CodeStepAsync(flow, "recovery-wrong@novacore.test");
        var response = await flow.SubmitCodeAsync(codePage, "BCDFG-HJKMN");

        Assert.Contains("Invalid recovery code.", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Equal(1, await users.GetAccessFailedCountAsync((await users.FindByIdAsync(user.Id.ToString()))!));
    }

    [Fact]
    public async Task A_code_of_digits_only_is_an_authenticator_code_whatever_its_length()
    {
        await _host.CreateUserAsync("mfa-digits@novacore.test", Password);
        await EnableAsync("mfa-digits@novacore.test");

        using var flow = new OidcFlow(_host);
        var (_, codePage) = await CodeStepAsync(flow, "mfa-digits@novacore.test");
        var response = await flow.SubmitCodeAsync(codePage, "12345");

        Assert.Contains("Invalid authenticator code.", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_locked_account_cannot_sign_in_with_a_recovery_code()
    {
        var user = await _host.CreateUserAsync("recovery-locked@novacore.test", Password);
        var (_, recoveryCodes) = await EnableAsync("recovery-locked@novacore.test");

        using var flow = new OidcFlow(_host);
        var (_, codePage) = await CodeStepAsync(flow, "recovery-locked@novacore.test");
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await users.SetLockoutEndDateAsync((await users.FindByIdAsync(user.Id.ToString()))!, DateTimeOffset.MaxValue);
        }

        var response = await flow.SubmitCodeAsync(codePage, recoveryCodes[0]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid recovery code.", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_code_step_needs_the_password_step_first()
    {
        using var flow = new OidcFlow(_host);

        var response = await flow.Browser.GetAsync("/account/login/2fa?ReturnUrl=%2F");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/account/login?", response.Headers.Location!.OriginalString, StringComparison.Ordinal);
    }

    // Signs in, sets up the authenticator and enables it with the right code; returns the shared key and the recovery codes.
    private async Task<(string Key, string[] RecoveryCodes)> EnableAsync(string email)
    {
        var (flow, accessToken) = await SignedInAsync(email);
        using var ownedFlow = flow;
        var key = await SetUpAsync(flow, accessToken);

        var enable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { code = AuthenticatorCode.For(key) });
        Assert.Equal(HttpStatusCode.OK, enable.StatusCode);
        var recoveryCodes = (await enable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("recoveryCodes")
            .EnumerateArray().Select(code => code.GetString()!).ToArray();
        Assert.Equal(10, recoveryCodes.Length);
        return (key, recoveryCodes);
    }

    // Passes the password step of an account with two-factor authentication; returns the PKCE verifier and the code page.
    private static async Task<(string Verifier, HttpResponseMessage CodePage)> CodeStepAsync(OidcFlow flow, string email)
    {
        var (verifier, afterPassword) = await flow.BeginSignInAsync(email, Password);
        Assert.StartsWith("/account/login/2fa", afterPassword.Headers.Location!.OriginalString, StringComparison.Ordinal);
        return (verifier, await flow.Browser.GetAsync(afterPassword.Headers.Location));
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
