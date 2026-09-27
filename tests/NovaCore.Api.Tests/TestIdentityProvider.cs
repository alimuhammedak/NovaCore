using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace NovaCore.Api.Tests;

/// <summary>
/// An in-memory OpenID Connect issuer: an RSA signing key, plus the discovery document and JWKS served by
/// <see cref="Handler"/>. The test host wires the handler as the JWT bearer backchannel, so tokens are validated through
/// the real Authority → metadata → JWKS path without a network (docs/plan/RESEARCH.md R-056).
/// </summary>
public sealed class TestIdentityProvider : IDisposable
{
    /// <summary>The issuer, and the <c>Jwt:Authority</c> the test host points at.</summary>
    public const string Issuer = "https://idp.novacore.test";

    /// <summary>The audience the host accepts (the <c>Jwt:Audience</c> default).</summary>
    public const string Audience = "novacore.api";

    private const string JwksPath = "/.well-known/jwks.json";

    private static readonly JsonWebTokenHandler Tokens = new();

    private readonly RSA _rsa = RSA.Create(2048);

    public TestIdentityProvider()
    {
        SigningKey = new RsaSecurityKey(_rsa) { KeyId = "novacore-test-key" };
        Handler = new MetadataHandler(SigningKey);
    }

    /// <summary>The key the discovery document publishes and <see cref="CreateToken"/> signs with by default.</summary>
    public RsaSecurityKey SigningKey { get; }

    /// <summary>Serves <c>/.well-known/openid-configuration</c> and the JWKS for <see cref="Issuer"/>.</summary>
    public HttpMessageHandler Handler { get; }

    /// <summary>
    /// A signed access token carrying <paramref name="claims"/>. The optional arguments let a test break one property
    /// at a time: the issuer, the audience, the signing key (and algorithm) or the expiry.
    /// </summary>
    public static string CreateToken(
        IEnumerable<Claim> claims,
        SecurityKey signingKey,
        string algorithm = SecurityAlgorithms.RsaSha256,
        string issuer = Issuer,
        string audience = Audience,
        DateTimeOffset? expires = null)
    {
        // JWT validation reads the real clock (docs/plan/RESEARCH.md R-058), so tokens are stamped with it too.
        var now = TimeProvider.System.GetUtcNow();
        var expiry = expires ?? now.AddMinutes(5);
        var issuedAt = expiry < now ? expiry.AddMinutes(-5) : now;

        return Tokens.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiry.UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey, algorithm)
        });
    }

    /// <summary>A token signed by this issuer's published key.</summary>
    public string CreateToken(params Claim[] claims) => CreateToken(claims, SigningKey);

    public void Dispose()
    {
        Handler.Dispose();
        _rsa.Dispose();
    }

    private sealed class MetadataHandler(RsaSecurityKey key) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath;
            object? document = path switch
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
