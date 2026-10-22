using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// What the provider must refuse (PLAN F6b.7; RFC 9700, RFC 6749, RFC 7636, RFC 9207): redirect URIs that aren't the
/// registered one, a code used twice, by another client or without its PKCE verifier, the implicit and password
/// grants; and what it must not show: private key parts, or its pages inside a frame.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class NegativeProtocolTests(NegativeProtocolTests.Fixture fixture) : IClassFixture<NegativeProtocolTests.Fixture>
{
    private const string Password = "a long passphrase for tests";
    private const string OtherClientId = "other-spa";
    private const string OtherRedirectUri = "https://other.novacore.test/callback";

    // A missing verifier is a malformed request, a wrong one a bad grant (RFC 7636 §4.6).
    private static readonly string[] PkceErrors = ["invalid_grant", "invalid_request"];

    // The JWK members of a private RSA or EC key, and of a symmetric one (RFC 7518 §6).
    private static readonly string[] PrivateKeyParts = ["d", "p", "q", "dp", "dq", "qi", "k"];

    private readonly IdentityServerTestHost _host = fixture.Host;

    /// <summary>A second public client, to redeem the first one's code with.</summary>
    public sealed class Fixture : IdentityServerFixture
    {
        protected override void Configure(WebApplicationBuilder builder) =>
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:Clients:1:ClientId"] = OtherClientId,
                ["Identity:Clients:1:RedirectUris:0"] = OtherRedirectUri
            });
    }

    // RFC 9700 §2.1: exact string matching, so no variant of the registered URI receives a code.
    [Theory]
    [InlineData("https://client.novacore.test/callback/")]
    [InlineData("https://client.novacore.test/callback?next=1")]
    [InlineData("https://client.novacore.test/Callback")]
    [InlineData("https://client.novacore.test:8443/callback")]
    [InlineData("http://client.novacore.test/callback")]
    [InlineData("https://attacker.novacore.test/callback")]
    [InlineData(OtherRedirectUri)]
    public async Task A_redirect_uri_other_than_the_registered_one_gets_no_redirect(string redirectUri)
    {
        using var flow = new OidcFlow(_host);

        var response = await flow.Browser.GetAsync(OidcFlow.AuthorizeUri(OidcFlow.NewPkce().Challenge, ("redirect_uri", redirectUri)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    // RFC 6749 §4.1.2: a code is used once; a second use is refused and revokes what the first one issued.
    [Fact]
    public async Task An_authorization_code_is_redeemed_once()
    {
        using var flow = new OidcFlow(_host);
        var (code, verifier) = await CodeAsync(flow, "code-once@novacore.test");
        var tokens = await flow.RedeemAsync(code, verifier);

        var again = await RedeemAsync(flow, code, verifier);

        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(again));
        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(await flow.RefreshAsync(tokens.RefreshToken)));
    }

    [Fact]
    public async Task An_authorization_code_is_redeemed_only_by_its_client()
    {
        using var flow = new OidcFlow(_host);
        var (code, verifier) = await CodeAsync(flow, "code-client@novacore.test");

        var response = await RedeemAsync(flow, code, verifier, OtherClientId, OtherRedirectUri);

        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(response));
    }

    // RFC 7636 §4.6: the code is bound to the challenge, so without the matching verifier it is worthless.
    [Theory]
    [InlineData(null)]
    [InlineData("a-verifier-that-does-not-match-the-challenge-0123456789")]
    public async Task An_authorization_code_needs_its_PKCE_verifier(string? verifier)
    {
        using var flow = new OidcFlow(_host);
        var (code, _) = await CodeAsync(flow, $"pkce-{Guid.NewGuid():N}@novacore.test");

        var response = await RedeemAsync(flow, code, verifier);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(await OidcFlow.ErrorFrom(response), PkceErrors);
    }

    // RFC 9700 §2.1.2 and §2.4: no implicit flow, no resource owner password credentials.
    [Fact]
    public async Task The_implicit_flow_is_refused()
    {
        using var flow = new OidcFlow(_host);

        var response = await flow.Browser.GetAsync(OidcFlow.AuthorizeUri(OidcFlow.NewPkce().Challenge, ("response_type", "token")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task The_password_grant_is_refused()
    {
        var user = await _host.CreateUserAsync("ropc@novacore.test", Password);
        using var flow = new OidcFlow(_host);

        var response = await flow.TokenRequestAsync(new()
        {
            ["grant_type"] = "password",
            ["username"] = user.Email!,
            ["password"] = Password
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("unsupported_grant_type", await OidcFlow.ErrorFrom(response));
    }

    // RFC 9207: the authorization response names its issuer, so a client talking to several can't be mixed up.
    [Fact]
    public async Task The_authorization_response_names_the_issuer()
    {
        using var flow = new OidcFlow(_host);
        var discovery = await (await flow.Browser.GetAsync("/.well-known/openid-configuration")).Content.ReadFromJsonAsync<JsonElement>();
        var (_, afterPassword) = await flow.BeginSignInAsync(
            (await _host.CreateUserAsync("iss@novacore.test", Password)).Email!, Password);

        var callback = await flow.Browser.GetAsync(afterPassword.Headers.Location);

        Assert.True(discovery.GetProperty("authorization_response_iss_parameter_supported").GetBoolean());
        Assert.Equal(discovery.GetProperty("issuer").GetString(), QueryHelpers.ParseQuery(callback.Headers.Location!.Query)["iss"].ToString());
    }

    [Fact]
    public async Task The_published_keys_hold_no_private_parts()
    {
        using var flow = new OidcFlow(_host);
        var discovery = await (await flow.Browser.GetAsync("/.well-known/openid-configuration")).Content.ReadFromJsonAsync<JsonElement>();

        var jwks = await (await flow.Browser.GetAsync(new Uri(discovery.GetProperty("jwks_uri").GetString()!).PathAndQuery))
            .Content.ReadFromJsonAsync<JsonElement>();

        var keys = jwks.GetProperty("keys").EnumerateArray().ToList();
        Assert.NotEmpty(keys);
        Assert.All(keys, key => Assert.All(PrivateKeyParts,
            part => Assert.False(key.TryGetProperty(part, out _), $"The published key carries '{part}'.")));
    }

    // Clickjacking: a page that takes a password or a code must not render inside another site's frame.
    [Theory]
    [InlineData("/account/login")]
    [InlineData("/account/forgot-password")]
    [InlineData("/account/reset-password?userId=x&token=y")]
    public async Task The_account_pages_cannot_be_framed(string path)
    {
        using var flow = new OidcFlow(_host);

        var response = await flow.Browser.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Contains("frame-ancestors 'none'", Assert.Single(response.Headers.GetValues("Content-Security-Policy")), StringComparison.Ordinal);
    }

    // Signs in through the browser and returns the code the authorization endpoint issues, with its PKCE verifier.
    private async Task<(string Code, string Verifier)> CodeAsync(OidcFlow flow, string email)
    {
        await _host.CreateUserAsync(email, Password);
        var (verifier, afterPassword) = await flow.BeginSignInAsync(email, Password);
        var callback = await flow.Browser.GetAsync(afterPassword.Headers.Location);
        return (OidcFlow.CodeFrom(callback), verifier);
    }

    private static Task<HttpResponseMessage> RedeemAsync(
        OidcFlow flow, string code, string? verifier, string clientId = IdentityServerTestHost.ClientId,
        string redirectUri = IdentityServerTestHost.RedirectUri)
    {
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId
        };
        if (verifier is not null)
            parameters["code_verifier"] = verifier;

        return flow.TokenRequestAsync(parameters);
    }
}
