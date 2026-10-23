using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using NovaCore.AspNetCore.Identity;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// PLAN F4c.1: secure by default. An endpoint without authorization metadata falls back to "authenticated callers only"
/// (the fallback policy, docs/plan/RESEARCH.md R-073), so a forgotten <c>RequirePermission</c> doesn't publish it. The
/// endpoints that must be public opt out with <c>AllowAnonymous</c>. Outside Development an empty
/// <c>Cors:AllowedOrigins</c> allows no origin.
/// </summary>
public sealed class SecureDefaultsTests(NovaCoreApiFactory factory, ProductionApiFactory production)
    : IClassFixture<NovaCoreApiFactory>, IClassFixture<ProductionApiFactory>
{
    private const string Origin = "https://app.novacore.test";

    [Fact]
    public async Task An_endpoint_without_authorization_metadata_requires_an_authenticated_caller()
    {
        await using var host = await CoreWebTestHost.StartAsync(app =>
        {
            app.MapGet("/plain", () => "plain");
            app.MapGet("/open", () => "open").AllowAnonymous();
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await host.CreateClient().GetAsync("/plain")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await host.CreateClient().GetAsync("/open")).StatusCode);

        using var authenticated = host.CreateClientWithClaims(new Claim(NovaClaims.Subject, Guid.NewGuid().ToString()));
        Assert.Equal(HttpStatusCode.OK, (await authenticated.GetAsync("/plain")).StatusCode);
    }

    // The identity provider's protocol endpoints and sign-in page are exercised anonymously by the IdentityServer tests.
    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar/v1")]
    [InlineData("/health")]
    [InlineData("/alive")]
    [InlineData("/account/login")]
    [InlineData("/account/forgot-password")]
    public async Task The_public_endpoints_of_the_host_stay_anonymous(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Outside_Development_no_allowed_origins_means_no_origin_is_allowed()
    {
        var client = production.CreateClient();

        using var simpleRequest = Request(HttpMethod.Get);
        using var preflightRequest = Preflight();
        using var simple = await client.SendAsync(simpleRequest);
        using var preflight = await client.SendAsync(preflightRequest);

        Assert.False(simple.Headers.Contains(HeaderNames.AccessControlAllowOrigin));
        Assert.False(preflight.Headers.Contains(HeaderNames.AccessControlAllowOrigin));
    }

    [Fact]
    public async Task In_Development_no_allowed_origins_means_any_origin_without_credentials()
    {
        var client = factory.CreateClient();

        using var request = Request(HttpMethod.Get);
        using var simple = await client.SendAsync(request);

        Assert.Equal("*", Assert.Single(simple.Headers.GetValues(HeaderNames.AccessControlAllowOrigin)));
    }

    private static HttpRequestMessage Request(HttpMethod method)
    {
        var request = new HttpRequestMessage(method, "/api/v1/products");
        request.Headers.Add(HeaderNames.Origin, Origin);
        return request;
    }

    private static HttpRequestMessage Preflight()
    {
        var request = Request(HttpMethod.Options);
        request.Headers.Add(HeaderNames.AccessControlRequestMethod, HttpMethods.Post);
        return request;
    }
}
