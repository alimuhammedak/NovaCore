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
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { ["Cors:AllowedOrigins:0"] = origin }));
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
        var page = await client.GetAsync(new Uri("/account/login", UriKind.Relative), TestContext.Current.CancellationToken);

        var antiforgery = Assert.Single(page.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith(".AspNetCore.Antiforgery", StringComparison.Ordinal));
        Assert.Contains("secure", antiforgery, StringComparison.OrdinalIgnoreCase);
    }

    // F6b.6c: OpenIddict answers revocation in its authentication handler, so no endpoint carries the "strict" policy
    // (R-103). The per-IP global window still counts it: the rate limiter runs before authentication.
    [Fact]
    public async Task Revocation_counts_toward_the_global_rate_limit()
    {
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { ["RateLimiting:PermitLimit"] = "3" }));
        using var client = host.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var request = 0; request < 4; request++)
        {
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["client_id"] = IdentityServerTestHost.ClientId,
                ["token"] = "not-a-token"
            });
            statuses.Add((await client.PostAsync(new Uri("/connect/revoke", UriKind.Relative), form, TestContext.Current.CancellationToken)).StatusCode);
        }

        // RFC 7009 §2.2: an unknown token is answered like a revoked one, so the answers reveal nothing until the limit.
        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests], statuses);
    }

    private string SignedByTheProvider(string subject, string type)
    {
        var server = _host.Services.GetRequiredService<IOptions<OpenIddictServerOptions>>().Value;
        // JWT validation reads the real clock (docs/plan/RESEARCH.md R-058).
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;

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
