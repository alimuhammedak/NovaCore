using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MyService.Api.Tests;

/// <summary>
/// An in-memory OpenID Connect issuer: an RSA signing key, plus the discovery document and JWKS served by
/// <see cref="Handler"/>. The test host uses the handler as the JWT bearer backchannel, so tokens are validated through
/// the real Authority → discovery → JWKS path, without a network.
/// </summary>
public sealed class TestIdentityProvider : IDisposable
{
    /// <summary>The issuer, and the <c>Jwt:Authority</c> the test host points at.</summary>
    public const string Issuer = "https://idp.test";

    /// <summary>The audience the host accepts (<c>Jwt:Audience</c>).</summary>
    public const string Audience = "myservice.api";

    private const string JwksPath = "/.well-known/jwks.json";

    private static readonly JsonWebTokenHandler Tokens = new();

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly TimeProvider _clock;

    /// <param name="clock">Stamps the tokens. JWT validation reads the real clock, so give it <see cref="TimeProvider.System"/>.</param>
    public TestIdentityProvider(TimeProvider clock)
    {
        _clock = clock;
        SigningKey = new RsaSecurityKey(_rsa) { KeyId = "test-key" };
        Handler = new MetadataHandler(SigningKey);
    }

    /// <summary>The key the discovery document publishes and <see cref="CreateToken"/> signs with.</summary>
    public RsaSecurityKey SigningKey { get; }

    /// <summary>Serves <c>/.well-known/openid-configuration</c> and the JWKS for <see cref="Issuer"/>.</summary>
    public HttpMessageHandler Handler { get; }

    /// <summary>An access token for the API carrying <paramref name="claims"/>, valid for five minutes.</summary>
    public string CreateToken(params Claim[] claims)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        return Tokens.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(5),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256)
        });
    }

    public void Dispose()
    {
        Handler.Dispose();
        _rsa.Dispose();
    }

    private sealed class MetadataHandler(RsaSecurityKey key) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            object? document = request.RequestUri?.AbsolutePath switch
            {
                "/.well-known/openid-configuration" => new Dictionary<string, object>
                {
                    ["issuer"] = Issuer,
                    ["jwks_uri"] = Issuer + JwksPath
                },
                JwksPath => Jwks(key),
                _ => null
            };

            var response = document is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(document) };
            return Task.FromResult(response);
        }

        private static Dictionary<string, object> Jwks(RsaSecurityKey key)
        {
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
            return new Dictionary<string, object>
            {
                ["keys"] = new[]
                {
                    new Dictionary<string, string>
                    {
                        ["kty"] = jwk.Kty,
                        ["use"] = "sig",
                        ["alg"] = SecurityAlgorithms.RsaSha256,
                        ["kid"] = jwk.Kid,
                        ["n"] = jwk.N,
                        ["e"] = jwk.E
                    }
                }
            };
        }
    }
}
