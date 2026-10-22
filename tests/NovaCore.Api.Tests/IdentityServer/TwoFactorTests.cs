using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            (await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { password = Password, code })).StatusCode);

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

        var enable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { password = Password, code = AuthenticatorCode.WrongFor(key) });

        Assert.Equal(HttpStatusCode.BadRequest, enable.StatusCode);
        Assert.True((await enable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("code", out _));
        Assert.False(await TwoFactorEnabledAsync(user.Id));
    }

    [Fact]
    public async Task A_wrong_code_at_the_api_counts_as_a_failed_access()
    {
        var user = await _host.CreateUserAsync("mfa-api-wrong@novacore.test", Password);
        var (flow, accessToken) = await SignedInAsync("mfa-api-wrong@novacore.test");
        using var ownedFlow = flow;
        var key = await SetUpAsync(flow, accessToken);

        await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { password = Password, code = AuthenticatorCode.WrongFor(key) });

        Assert.Equal(1, await AccessFailedCountAsync(user.Id));
    }

    // ASVS 7.5.1: an access token alone doesn't change the second factor. A wrong password counts toward lockout.
    [Fact]
    public async Task Setup_needs_the_current_password()
    {
        var user = await _host.CreateUserAsync("mfa-setup-password@novacore.test", Password);
        var (flow, accessToken) = await SignedInAsync("mfa-setup-password@novacore.test");
        using var ownedFlow = flow;

        var setup = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/setup", accessToken, new { password = "not the password" });

        Assert.Equal(HttpStatusCode.BadRequest, setup.StatusCode);
        var json = await setup.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("errors").TryGetProperty("password", out _));
        Assert.False(json.TryGetProperty("sharedKey", out _));
        Assert.Equal(1, await AccessFailedCountAsync(user.Id));
    }

    [Fact]
    public async Task Enabling_needs_the_current_password()
    {
        var user = await _host.CreateUserAsync("mfa-enable-password@novacore.test", Password);
        var (flow, accessToken) = await SignedInAsync("mfa-enable-password@novacore.test");
        using var ownedFlow = flow;
        var key = await SetUpAsync(flow, accessToken);

        var enable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken,
            new { password = "not the password", code = AuthenticatorCode.For(key) });

        Assert.Equal(HttpStatusCode.BadRequest, enable.StatusCode);
        Assert.True((await enable.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("password", out _));
        Assert.False(await TwoFactorEnabledAsync(user.Id));
        Assert.Equal(1, await AccessFailedCountAsync(user.Id));
    }

    [Fact]
    public async Task Disabling_needs_the_current_password()
    {
        var user = await _host.CreateUserAsync("mfa-disable-password@novacore.test", Password);
        var (key, _) = await EnableAsync("mfa-disable-password@novacore.test");
        using var flow = new OidcFlow(_host);
        var (verifier, codePage) = await CodeStepAsync(flow, "mfa-disable-password@novacore.test");
        var tokens = await flow.FinishSignInAsync(await flow.SubmitCodeAsync(codePage, AuthenticatorCode.For(key, stepsAhead: 1)), verifier);

        var disable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/disable", tokens.AccessToken,
            new { password = "not the password", code = AuthenticatorCode.For(key, stepsAhead: 2) });

        Assert.Equal(HttpStatusCode.BadRequest, disable.StatusCode);
        Assert.True(await TwoFactorEnabledAsync(user.Id));
    }

    [Fact]
    public async Task Enabling_again_replaces_the_recovery_codes()
    {
        await _host.CreateUserAsync("recovery-new@novacore.test", Password);
        var (key, oldCodes) = await EnableAsync("recovery-new@novacore.test");
        using var flow = new OidcFlow(_host);
        var (verifier, codePage) = await CodeStepAsync(flow, "recovery-new@novacore.test");
        var tokens = await flow.FinishSignInAsync(await flow.SubmitCodeAsync(codePage, AuthenticatorCode.For(key, stepsAhead: 1)), verifier);

        var again = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", tokens.AccessToken,
            new { password = Password, code = AuthenticatorCode.For(key, stepsAhead: 2) });

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        var newCodes = (await again.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("recoveryCodes")
            .EnumerateArray().Select(code => code.GetString()!).ToArray();
        Assert.Equal(10, newCodes.Length);
        Assert.Empty(newCodes.Intersect(oldCodes));

        using var withOld = new OidcFlow(_host);
        var (_, oldCodePage) = await CodeStepAsync(withOld, "recovery-new@novacore.test");
        Assert.Contains("Invalid recovery code.",
            await (await withOld.SubmitCodeAsync(oldCodePage, oldCodes[0])).Content.ReadAsStringAsync(), StringComparison.Ordinal);

        using var withNew = new OidcFlow(_host);
        var (newVerifier, newCodePage) = await CodeStepAsync(withNew, "recovery-new@novacore.test");
        Assert.False(string.IsNullOrEmpty(
            (await withNew.FinishSignInAsync(await withNew.SubmitCodeAsync(newCodePage, newCodes[0]), newVerifier)).AccessToken));
    }

    // A session that started without the second factor, or with the factor being removed, doesn't outlive the change.
    [Fact]
    public async Task Turning_two_factor_authentication_on_or_off_ends_the_sessions()
    {
        await _host.CreateUserAsync("mfa-sessions@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var before = await flow.SignInAsync("mfa-sessions@novacore.test", Password);
        var key = await SetUpAsync(flow, before.AccessToken);

        Assert.Equal(HttpStatusCode.OK, (await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", before.AccessToken,
            new { password = Password, code = AuthenticatorCode.For(key) })).StatusCode);
        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(await flow.RefreshAsync(before.RefreshToken)));

        using var second = new OidcFlow(_host);
        var (verifier, codePage) = await CodeStepAsync(second, "mfa-sessions@novacore.test");
        var during = await second.FinishSignInAsync(await second.SubmitCodeAsync(codePage, AuthenticatorCode.For(key, stepsAhead: 1)), verifier);

        Assert.Equal(HttpStatusCode.NoContent, (await second.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/disable", during.AccessToken,
            new { password = Password, code = AuthenticatorCode.For(key, stepsAhead: 2) })).StatusCode);
        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(await second.RefreshAsync(during.RefreshToken)));
    }

    // F6b.1d: the Administrator role counts once the account has the second factor; the change applies to the same token.
    [Fact]
    public async Task An_administrator_needs_two_factor_authentication()
    {
        var user = await _host.CreateUserAsync("admin-2fa@novacore.test", Password);
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.AddToRoleAsync((await users.FindByIdAsync(user.Id.ToString()))!, "Administrator")).Succeeded);
        }

        var (flow, accessToken) = await SignedInAsync("admin-2fa@novacore.test");
        using var ownedFlow = flow;
        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync("/api/v1/admin/users", accessToken)).StatusCode);

        var key = await SetUpAsync(flow, accessToken);
        Assert.Equal(HttpStatusCode.OK, (await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken,
            new { password = Password, code = AuthenticatorCode.For(key) })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await flow.CallApiAsync("/api/v1/admin/users", accessToken)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/disable", accessToken,
            new { password = Password, code = AuthenticatorCode.For(key, stepsAhead: 1) })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync("/api/v1/admin/users", accessToken)).StatusCode);
    }

    [Fact]
    public async Task A_malformed_code_is_a_validation_error()
    {
        await _host.CreateUserAsync("mfa-short@novacore.test", Password);
        var (flow, accessToken) = await SignedInAsync("mfa-short@novacore.test");
        using var ownedFlow = flow;

        var enable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { password = Password, code = "12345" });

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
        await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { password = Password, code = AuthenticatorCode.For(key) });

        var disable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/disable", accessToken, new { password = Password, code = AuthenticatorCode.For(key, stepsAhead: 1) });

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
            (await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { password = Password, code = AuthenticatorCode.For(key) })).StatusCode);

        // With the key, a stolen access token could compute codes and turn two-factor authentication off.
        var again = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/setup", accessToken, new { password = Password });

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
    public async Task Recovery_codes_are_stored_hashed()
    {
        var user = await _host.CreateUserAsync("recovery-hashed@novacore.test", Password);
        var (_, recoveryCodes) = await EnableAsync("recovery-hashed@novacore.test");

        await using var scope = _host.Services.CreateAsyncScope();
        var tokens = await scope.ServiceProvider.GetRequiredService<IdentityServerDbContext>().UserTokens
            .Where(token => token.UserId == user.Id)
            .Select(token => token.Value)
            .ToListAsync(TestContext.Current.CancellationToken);

        // ASVS 6.5.2: a code read from the store doesn't sign in.
        Assert.All(recoveryCodes, code => Assert.DoesNotContain(tokens, value => value != null && value.Contains(code, StringComparison.Ordinal)));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Equal(10, await users.CountRecoveryCodesAsync((await users.FindByIdAsync(user.Id.ToString()))!));
    }

    // Codes from 0.1.0-preview.1 sit in Identity's own plain-text slot; new codes take their place there too.
    [Fact]
    public async Task New_recovery_codes_remove_the_plain_text_ones_of_an_earlier_version()
    {
        var user = await _host.CreateUserAsync("recovery-legacy@novacore.test", Password);
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var account = (await users.FindByIdAsync(user.Id.ToString()))!;
            Assert.True((await users.SetAuthenticationTokenAsync(account, "[AspNetUserStore]", "RecoveryCodes", "ABCDE-FGHJK;BCDFG-HJKMN")).Succeeded);
        }

        await EnableAsync("recovery-legacy@novacore.test");

        await using var check = _host.Services.CreateAsyncScope();
        var store = check.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Null(await store.GetAuthenticationTokenAsync((await store.FindByIdAsync(user.Id.ToString()))!, "[AspNetUserStore]", "RecoveryCodes"));
    }

    [Fact]
    public async Task A_redeemed_recovery_code_is_no_longer_counted()
    {
        var user = await _host.CreateUserAsync("recovery-count@novacore.test", Password);
        var (_, recoveryCodes) = await EnableAsync("recovery-count@novacore.test");

        using var flow = new OidcFlow(_host);
        var (verifier, codePage) = await CodeStepAsync(flow, "recovery-count@novacore.test");
        await flow.FinishSignInAsync(await flow.SubmitCodeAsync(codePage, recoveryCodes[3]), verifier);

        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Equal(9, await users.CountRecoveryCodesAsync((await users.FindByIdAsync(user.Id.ToString()))!));
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

        var enable = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { password = Password, code = AuthenticatorCode.For(key) });
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
        var setup = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/setup", accessToken, new { password = Password });
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        var json = await setup.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("otpauth://totp/", json.GetProperty("authenticatorUri").GetString(), StringComparison.Ordinal);
        return json.GetProperty("sharedKey").GetString()!;
    }

    private async Task<int> AccessFailedCountAsync(Guid userId)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.GetAccessFailedCountAsync((await users.FindByIdAsync(userId.ToString()))!);
    }

    private async Task<bool> TwoFactorEnabledAsync(Guid userId)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.GetTwoFactorEnabledAsync((await users.FindByIdAsync(userId.ToString()))!);
    }
}
