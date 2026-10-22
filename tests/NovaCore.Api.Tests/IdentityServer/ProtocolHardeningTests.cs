using System.Net;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NovaCore.AspNetCore.Identity;
using OpenIddict.Server;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The provider's token and protocol hardening (PLAN F6b.4, DECISIONS D-053 (8)–(9)).
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
