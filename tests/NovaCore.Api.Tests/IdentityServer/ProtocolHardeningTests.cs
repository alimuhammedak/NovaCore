using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NovaCore.AspNetCore.Identity;
using OpenIddict.Server;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The provider's token and protocol hardening (PLAN F6b.4 and F6b.6, DECISIONS D-053 (8)–(9)).
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class ProtocolHardeningTests(IdentityServerFixture fixture) : IClassFixture<IdentityServerFixture>
{
    private const string Password = "a long passphrase for tests";

    private readonly IdentityServerTestHost _host = fixture.Host;

    // RFC 9068 §4: a resource server takes only "at+jwt" from a provider that issues them, so another JWT signed with
    // the same key (an identity token, say) isn't an access token. The control token differs only in its type.
    [Theory]
    [InlineData("at+jwt", HttpStatusCode.OK)]
    [InlineData("JWT", HttpStatusCode.Unauthorized)]
    public async Task Only_an_at_jwt_token_is_an_access_token(string type, HttpStatusCode expected)
    {
        var user = await _host.CreateUserAsync($"typ-{Guid.NewGuid():N}@novacore.test", Password);
        using var flow = new OidcFlow(_host);

        var response = await flow.CallApiAsync("/api/v1/auth/me", SignedByTheProvider(user.Id.ToString(), type));

        Assert.Equal(expected, response.StatusCode);
    }

    // RFC 9700 §2.6: the authorization endpoint and the sign-in pages are for browsers navigating, not for scripts on
    // other origins. With configured origins the core's policy allows credentials, so a script there could read the
    // pages with the user's cookies. The token endpoint keeps CORS: a single-page app redeems its code there.
    [Theory]
    [InlineData("/connect/authorize", false)]
    [InlineData("/account/login", false)]
    [InlineData("/account/forgot-password", false)]
    [InlineData("/connect/token", true)]
    [InlineData("/api/v1/auth/me", true)]
    public async Task The_sign_in_endpoints_answer_no_cross_origin_request(string path, bool allowed)
    {
        const string origin = "https://app.novacore.test";
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin }));
        using var client = host.CreateClient();
        using var preflight = new HttpRequestMessage(HttpMethod.Options, path);
        preflight.Headers.Add("Origin", origin);
        preflight.Headers.Add("Access-Control-Request-Method", path.StartsWith("/account", StringComparison.Ordinal) ? "GET" : "POST");

        var response = await client.SendAsync(preflight, TestContext.Current.CancellationToken);

        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    // R-104: the antiforgery cookie had no Secure flag, not even over https, and Identity's two-factor cookies follow
    // the request's scheme, which behind a TLS proxy without forwarded headers is http. The application cookie was
    // Always already. Outside Development antiforgery is Always too (IdentityServerConfigurationTests).
    [Fact]
    public async Task The_sign_in_cookies_are_secure()
    {
        var cookies = _host.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        Assert.Equal(CookieSecurePolicy.Always, cookies.Get(IdentityConstants.TwoFactorUserIdScheme).Cookie.SecurePolicy);
        Assert.Equal(CookieSecurePolicy.Always, cookies.Get(IdentityConstants.TwoFactorRememberMeScheme).Cookie.SecurePolicy);

        using var client = _host.CreateClient();
        var page = await client.GetAsync("/account/login", TestContext.Current.CancellationToken);

        var antiforgery = Assert.Single(page.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith(".AspNetCore.Antiforgery", StringComparison.Ordinal));
        Assert.Contains("secure", antiforgery, StringComparison.OrdinalIgnoreCase);
    }

    private string SignedByTheProvider(string subject, string type)
    {
        var server = _host.Services.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
        var now = DateTime.UtcNow;

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            TokenType = type,
            Issuer = server.Issuer!.AbsoluteUri,
            Audience = "novacore.api",
            Subject = new ClaimsIdentity([new Claim(NovaClaims.Subject, subject)]),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(5),
            SigningCredentials = server.SigningCredentials.First(credentials => credentials.Key is AsymmetricSecurityKey)
        });
    }
}
