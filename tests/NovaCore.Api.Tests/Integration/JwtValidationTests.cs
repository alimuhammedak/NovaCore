using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NovaCore.AspNetCore.Identity;
using NovaCore.Modules.Catalog.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// JWT bearer validation against an OpenID Connect authority (PLAN F3a.1, D-002): the signing keys come from the
/// authority's discovery document and JWKS, and a token must carry that issuer, the configured audience, a valid
/// signature and an unexpired lifetime. A super-admin token reaches a permission-guarded endpoint (200); every broken
/// token is turned away as unauthenticated (401).
/// </summary>
public sealed class JwtValidationTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>, IAsyncLifetime
{
    private const string ProtectedEndpoint = "/api/v1/products";

    private readonly NovaCoreApiFactory _factory = factory;

    public async ValueTask InitializeAsync() => await EnsureCatalogCreatedAsync(_factory);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_token_signed_by_the_authority_is_accepted()
    {
        var token = _factory.IdentityProvider.CreateToken(SuperAdmin());

        var response = await GetWithTokenAsync(_factory, token);

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
        var signingKey = _factory.IdentityProvider.SigningKey;
        var token = defect switch
        {
            "wrong issuer" => TestIdentityProvider.CreateToken(SuperAdmin(), signingKey, issuer: "https://attacker.example"),
            "wrong audience" => TestIdentityProvider.CreateToken(SuperAdmin(), signingKey, audience: "another.api"),
            // Same key id, different key: the signature cannot be verified with the published key.
            "foreign signing key" => TestIdentityProvider.CreateToken(
                SuperAdmin(), new RsaSecurityKey(foreignRsa) { KeyId = signingKey.KeyId }),
            "expired" => TestIdentityProvider.CreateToken(
                SuperAdmin(), signingKey, expires: TimeProvider.System.GetUtcNow().AddMinutes(-10)),
            _ => throw new ArgumentOutOfRangeException(nameof(defect), defect, null)
        };

        var response = await GetWithTokenAsync(_factory, token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Outside_development_the_authority_is_the_only_trusted_issuer()
    {
        await using var factory = new ProductionApiFactory();
        await EnsureCatalogCreatedAsync(factory);

        // The authority's token works over the HTTPS-only metadata path Production requires...
        var accepted = await GetWithTokenAsync(factory, factory.IdentityProvider.CreateToken(SuperAdmin()));

        // ...while an HS256 token signed with the Development fallback key (a public constant in
        // AuthenticationExtensions) is not: symmetric keys are Development-only (D-024).
        var developmentKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("novacore-development-only-signing-key-do-not-use-in-production-0123456789"));
        var symmetric = TestIdentityProvider.CreateToken(
            SuperAdmin(), developmentKey, SecurityAlgorithms.HmacSha256, issuer: "novacore");
        var rejected = await GetWithTokenAsync(factory, symmetric);

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
    }

    private static Claim[] SuperAdmin() =>
    [
        new(NovaClaims.Subject, Guid.NewGuid().ToString()),
        new(NovaClaims.Role, HttpContextCurrentUser.SuperAdminRole)
    ];

    private static async Task<HttpResponseMessage> GetWithTokenAsync<TEntryPoint>(
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint> factory, string token)
        where TEntryPoint : class
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.GetAsync(ProtectedEndpoint);
    }

    private static async Task EnsureCatalogCreatedAsync<TEntryPoint>(
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint> factory)
        where TEntryPoint : class
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.EnsureCreatedAsync();
    }
}
