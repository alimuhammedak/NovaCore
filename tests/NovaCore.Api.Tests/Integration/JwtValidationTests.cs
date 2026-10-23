using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NovaCore.AspNetCore.Identity;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// JWT bearer validation against an OpenID Connect authority (PLAN F3a.1, D-002): the signing keys come from the
/// authority's discovery document and JWKS, and a token must carry that issuer, the configured audience, a valid
/// signature and an unexpired lifetime. A valid token of a caller holding the permission reaches a permission-guarded
/// endpoint (200); every broken token is turned away as unauthenticated (401).
/// </summary>
public sealed class JwtValidationTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>, IAsyncLifetime
{
    private const string ProtectedEndpoint = "/api/v1/products";

    public async ValueTask InitializeAsync() => await EnsureCatalogCreatedAsync(factory);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_token_signed_by_the_authority_is_accepted()
    {
        var token = factory.IdentityProvider.CreateToken(Reader(factory));

        var response = await GetWithTokenAsync(factory, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("wrong issuer")]
    [InlineData("wrong audience")]
    [InlineData("foreign signing key")]
    [InlineData("expired")]
    public async Task A_token_that_fails_validation_is_rejected(string defect)
    {
        using var foreignRsa = RSA.Create(2048);
        var signingKey = factory.IdentityProvider.SigningKey;
        var token = defect switch
        {
            "wrong issuer" => TestIdentityProvider.CreateToken(Reader(factory), signingKey, issuer: "https://attacker.example"),
            "wrong audience" => TestIdentityProvider.CreateToken(Reader(factory), signingKey, audience: "another.api"),
            // Same key id, different key: the signature cannot be verified with the published key.
            "foreign signing key" => TestIdentityProvider.CreateToken(
                Reader(factory), new RsaSecurityKey(foreignRsa) { KeyId = signingKey.KeyId }),
            "expired" => TestIdentityProvider.CreateToken(
                Reader(factory), signingKey, expires: TimeProvider.System.GetUtcNow().AddMinutes(-10)),
            _ => throw new ArgumentOutOfRangeException(nameof(defect), defect, null)
        };

        var response = await GetWithTokenAsync(factory, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Outside_development_the_authority_is_trusted_over_https_metadata()
    {
        await using var production = new ProductionApiFactory();
        await EnsureCatalogCreatedAsync(production);

        var accepted = await GetWithTokenAsync(production, production.IdentityProvider.CreateToken(Reader(production)));

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task A_symmetrically_signed_token_is_rejected_in_every_environment(string environment)
    {
        // Only the authority's published asymmetric keys sign tokens (DECISIONS D-028): an HS256 token with the trusted
        // issuer and audience is still refused, in Development too.
        await using var production = string.Equals(environment, "Production", StringComparison.Ordinal) ? new ProductionApiFactory() : null;
        var host = production ?? factory;
        await EnsureCatalogCreatedAsync(host);
        var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-symmetric-key-that-is-long-enough-0123456789"));

        var response = await GetWithTokenAsync(host,
            TestIdentityProvider.CreateToken(Reader(host), symmetricKey, SecurityAlgorithms.HmacSha256));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // A fresh subject that may read the protected endpoint, granted on the server of the given host.
    private static Claim[] Reader(NovaCoreApiFactory factory)
    {
        var subject = Guid.NewGuid().ToString();
        factory.GrantPermissions(subject, CatalogPermissions.ProductsRead);
        return [new(NovaClaims.Subject, subject)];
    }

    private static async Task<HttpResponseMessage> GetWithTokenAsync<TEntryPoint>(
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint> factory, string token)
        where TEntryPoint : class
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.GetAsync(new Uri(ProtectedEndpoint, UriKind.Relative));
    }

    private static async Task EnsureCatalogCreatedAsync<TEntryPoint>(
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint> factory)
        where TEntryPoint : class
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.EnsureCreatedAsync();
    }
}
