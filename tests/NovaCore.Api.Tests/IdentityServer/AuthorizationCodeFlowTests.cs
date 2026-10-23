using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using NovaCore.Identity.OpenIddict;
using OpenIddict.Server;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Authorization Code + PKCE end to end (PLAN F3b.1): a browser signs in on the provider's page, the client redeems the
/// code, and the core accepts the access token through <c>Jwt:Authority</c> and resolves the caller's permissions on the
/// server. Refresh tokens rotate, and a reused one revokes its whole chain.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class AuthorizationCodeFlowTests(AuthorizationCodeFlowTests.Fixture fixture)
    : IClassFixture<AuthorizationCodeFlowTests.Fixture>
{
    private const string Password = "Passw0rd!-for-tests";

    private readonly IdentityServerTestHost _host = fixture.Host;

    /// <summary>No reuse leeway, so a replayed refresh token counts as reuse at once (the default allows 30 s, R-059).</summary>
    public sealed class Fixture : IdentityServerFixture
    {
        protected override void Configure(WebApplicationBuilder builder) =>
            builder.Services.Configure<OpenIddictServerOptions>(server => server.RefreshTokenReuseLeeway = TimeSpan.Zero);
    }

    [Fact]
    public async Task The_flow_issues_an_access_token_the_core_accepts()
    {
        var user = await _host.CreateUserAsync("reader@novacore.test", Password, IdentityServerTestHost.ProbePermission);
        using var flow = new OidcFlow(_host);

        var tokens = await flow.SignInAsync("reader@novacore.test", Password);

        var probe = await flow.CallApiAsync("/probe", tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, probe.StatusCode);

        var accessToken = new JsonWebToken(tokens.AccessToken);
        Assert.Equal("RS256", accessToken.Alg);
        Assert.Equal("at+jwt", accessToken.Typ);
        Assert.Equal(IdentityServerTestHost.Authority + "/", accessToken.Issuer);
        Assert.Equal(["novacore.api"], accessToken.Audiences);
        Assert.Equal(user.Id.ToString(), accessToken.Subject);
        // Permissions are resolved on the server, never carried by the token.
        Assert.False(accessToken.TryGetClaim("permission", out _));
    }

    [Fact]
    public async Task A_user_without_the_permission_is_forbidden()
    {
        await _host.CreateUserAsync("nobody@novacore.test", Password);
        using var flow = new OidcFlow(_host);

        var tokens = await flow.SignInAsync("nobody@novacore.test", Password);

        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync("/probe", tokens.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task The_API_answers_an_anonymous_call_with_401_not_a_sign_in_redirect()
    {
        // The provider's cookie schemes sit next to the API's bearer default without replacing it.
        var response = await _host.CreateClient().GetAsync("/probe");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("code_challenge_method", "plain")]
    public async Task An_authorization_request_without_an_S256_challenge_is_rejected(string? parameter, string? value)
    {
        using var flow = new OidcFlow(_host);
        var challenge = parameter is null ? null : OidcFlow.NewPkce().Challenge;

        var response = await flow.Browser.GetAsync(OidcFlow.AuthorizeUri(challenge, (parameter ?? "unused", value)));

        // Rejected before the redirect URI is trusted, so OpenIddict answers itself instead of redirecting a code.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("error:invalid_request", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Prompt_none_without_a_session_returns_login_required()
    {
        using var flow = new OidcFlow(_host);

        var response = await flow.Browser.GetAsync(OidcFlow.AuthorizeUri(OidcFlow.NewPkce().Challenge, ("prompt", "none")));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("login_required", QueryHelpers.ParseQuery(response.Headers.Location!.Query)["error"].ToString());
    }

    [Fact]
    public async Task Sign_in_never_redirects_to_another_site()
    {
        await _host.CreateUserAsync("redirect@novacore.test", Password);
        using var flow = new OidcFlow(_host);

        var loginPage = await flow.Browser.GetAsync("/account/login?ReturnUrl=https%3A%2F%2Fevil.example%2F");
        var response = await flow.SubmitLoginAsync(loginPage, "redirect@novacore.test", Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("You are signed in", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_sign_in_form_requires_its_antiforgery_token()
    {
        await _host.CreateUserAsync("csrf@novacore.test", Password);
        using var flow = new OidcFlow(_host);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "csrf@novacore.test",
            ["Password"] = Password,
            ["ReturnUrl"] = "/"
        });
        var response = await flow.Browser.PostAsync("/account/login", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_refresh_token()
    {
        await _host.CreateUserAsync("rotate@novacore.test", Password, IdentityServerTestHost.ProbePermission);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("rotate@novacore.test", Password);

        var refreshed = await OidcFlow.TokensFrom(await flow.RefreshAsync(tokens.RefreshToken));

        Assert.NotEqual(tokens.RefreshToken, refreshed.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, (await flow.CallApiAsync("/probe", refreshed.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Reusing_a_redeemed_refresh_token_revokes_the_whole_chain()
    {
        await _host.CreateUserAsync("reuse@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("reuse@novacore.test", Password);
        var rotated = await OidcFlow.TokensFrom(await flow.RefreshAsync(tokens.RefreshToken));

        var replay = await flow.RefreshAsync(tokens.RefreshToken);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(replay));

        // The legitimate successor dies with it: the replay may have been the thief.
        var successor = await flow.RefreshAsync(rotated.RefreshToken);
        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(successor));
    }

    [Fact]
    public async Task A_revoked_refresh_token_is_refused()
    {
        await _host.CreateUserAsync("revoke@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("revoke@novacore.test", Password);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = tokens.RefreshToken,
            ["token_type_hint"] = "refresh_token",
            ["client_id"] = IdentityServerTestHost.ClientId
        });
        var revoke = await flow.Browser.PostAsync("/connect/revoke", form);
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(await flow.RefreshAsync(tokens.RefreshToken)));
    }

    [Fact]
    public async Task A_locked_out_user_cannot_refresh()
    {
        var user = await _host.CreateUserAsync("locked@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("locked@novacore.test", Password);

        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await users.SetLockoutEndDateAsync((await users.FindByIdAsync(user.Id.ToString()))!, DateTimeOffset.MaxValue);
        }

        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(await flow.RefreshAsync(tokens.RefreshToken)));
    }

    [Fact]
    public async Task End_session_signs_the_browser_out_of_the_provider()
    {
        await _host.CreateUserAsync("leave@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        await flow.SignInAsync("leave@novacore.test", Password);

        // The provider asks first; the confirmation posts the request back with its antiforgery token.
        var confirmation = await flow.Browser.GetAsync(EndSessionUri);
        var endSession = await flow.SubmitFormAsync(confirmation, "/connect/endsession", []);
        Assert.Equal(HttpStatusCode.Redirect, endSession.StatusCode);
        Assert.StartsWith(IdentityServerTestHost.PostLogoutRedirectUri, endSession.Headers.Location!.ToString(), StringComparison.Ordinal);

        // The next authorization request needs a new sign-in.
        Assert.StartsWith(IdentityServerTestHost.Authority + "/account/login", (await AuthorizeAsync(flow)).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task End_session_asks_before_signing_out()
    {
        await _host.CreateUserAsync("ask@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        await flow.SignInAsync("ask@novacore.test", Password);

        // A link on another site leads here too (logout CSRF): it gets the question, not the sign-out.
        var response = await flow.Browser.GetAsync(EndSessionUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Sign out", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.StartsWith(IdentityServerTestHost.RedirectUri, (await AuthorizeAsync(flow)).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_sign_out_post_without_the_confirmation_token_keeps_the_session()
    {
        await _host.CreateUserAsync("cross-site@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        await flow.SignInAsync("cross-site@novacore.test", Password);

        // What a form on another site can post: the parameters, but no antiforgery token.
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = IdentityServerTestHost.ClientId,
            ["post_logout_redirect_uri"] = IdentityServerTestHost.PostLogoutRedirectUri
        });
        var response = await flow.Browser.PostAsync("/connect/endsession", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith(IdentityServerTestHost.RedirectUri, (await AuthorizeAsync(flow)).ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_signed_out_browser_is_sent_on_without_the_question()
    {
        using var flow = new OidcFlow(_host);

        var response = await flow.Browser.GetAsync(EndSessionUri);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(IdentityServerTestHost.PostLogoutRedirectUri, response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    private static string EndSessionUri => QueryHelpers.AddQueryString("/connect/endsession", new Dictionary<string, string?>
    {
        ["client_id"] = IdentityServerTestHost.ClientId,
        ["post_logout_redirect_uri"] = IdentityServerTestHost.PostLogoutRedirectUri
    });

    // Where a new authorization request leads: the client's callback while the provider session lasts, else the sign-in page.
    private static async Task<Uri> AuthorizeAsync(OidcFlow flow)
    {
        var response = await flow.Browser.GetAsync(OidcFlow.AuthorizeUri(OidcFlow.NewPkce().Challenge));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location!;
    }
}

/// <summary>
/// The age of a sign-in (OpenID Connect Core §2 <c>auth_time</c>, §3.1.2.1 <c>max_age</c>) counts from the sign-in, not
/// from the provider cookie's last renewal, which moves with sliding expiration and every security stamp check.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class SignInAgeTests(FakeClockIdentityServerFixture fixture) : IClassFixture<FakeClockIdentityServerFixture>
{
    private const string Password = "a long passphrase for tests";

    [Fact]
    public async Task Auth_time_and_max_age_count_from_the_sign_in_not_the_cookie_renewal()
    {
        var host = fixture.Host;
        await host.CreateUserAsync("age@novacore.test", Password);
        using var flow = new OidcFlow(host);
        var signedInAt = fixture.Clock.GetUtcNow();
        await flow.SignInAsync("age@novacore.test", Password);

        // Past half of the cookie's 30 minutes, so this request renews the cookie.
        fixture.Clock.Advance(TimeSpan.FromMinutes(16));
        Assert.StartsWith(IdentityServerTestHost.RedirectUri, (await AuthorizeAsync(flow, OidcFlow.NewPkce().Challenge)).ToString(), StringComparison.Ordinal);

        // 21 minutes after the sign-in and 5 after the renewal.
        fixture.Clock.Advance(TimeSpan.FromMinutes(5));
        var (verifier, challenge) = OidcFlow.NewPkce();
        var callback = await flow.Browser.GetAsync(OidcFlow.AuthorizeUri(challenge));
        var tokens = await flow.RedeemAsync(OidcFlow.CodeFrom(callback), verifier);
        Assert.Equal(
            signedInAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            new JsonWebToken(tokens.IdToken!).GetClaim("auth_time").Value);

        var tooOld = await AuthorizeAsync(flow, OidcFlow.NewPkce().Challenge, ("max_age", "1200"));
        Assert.StartsWith(IdentityServerTestHost.Authority + "/account/login", tooOld.ToString(), StringComparison.Ordinal);
    }

    private static async Task<Uri> AuthorizeAsync(OidcFlow flow, string challenge, params (string Name, string? Value)[] extra)
    {
        var response = await flow.Browser.GetAsync(OidcFlow.AuthorizeUri(challenge, extra));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location!;
    }
}
