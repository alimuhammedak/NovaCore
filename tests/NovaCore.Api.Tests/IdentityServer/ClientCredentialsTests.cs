using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Client credentials (PLAN F3b.1): a service client authenticates with its secret, its token's <c>sub</c> is the client
/// id, and it holds the permissions configured for it in <c>Identity:Clients</c>.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class ClientCredentialsTests(ClientCredentialsTests.Fixture fixture) : IClassFixture<ClientCredentialsTests.Fixture>
{
    private readonly IdentityServerTestHost _host = fixture.Host;

    /// <summary>Two service clients next to the host's user-facing one: one granted the probe permission, one not.</summary>
    public sealed class Fixture : IdentityServerFixture
    {
        protected override void Configure(WebApplicationBuilder builder) =>
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:Clients:1:ClientId"] = "worker",
                ["Identity:Clients:1:ClientSecret"] = "worker-secret-for-tests-at-least-32-chars",
                ["Identity:Clients:1:Permissions:0"] = IdentityServerTestHost.ProbePermission,
                ["Identity:Clients:2:ClientId"] = "reporter",
                ["Identity:Clients:2:ClientSecret"] = "reporter-secret-for-tests-at-least-32-chars"
            });
    }

    [Fact]
    public async Task A_service_client_holds_the_permissions_configured_for_it()
    {
        var token = await TokenAsync("worker", "worker-secret-for-tests-at-least-32-chars");

        Assert.Equal("worker", new JsonWebToken(token).Subject);
        Assert.Equal(HttpStatusCode.OK, (await CallProbeAsync(token)).StatusCode);
    }

    [Fact]
    public async Task A_service_client_without_the_permission_is_forbidden()
    {
        var token = await TokenAsync("reporter", "reporter-secret-for-tests-at-least-32-chars");

        Assert.Equal(HttpStatusCode.Forbidden, (await CallProbeAsync(token)).StatusCode);
    }

    [Fact]
    public async Task A_wrong_secret_is_rejected()
    {
        var response = await RequestTokenAsync("worker", "not-the-secret");

        // RFC 6749 §5.2: a failed client authentication may answer 401.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("invalid_client", await OidcFlow.ErrorFrom(response));
    }

    [Fact]
    public async Task A_user_facing_client_cannot_use_client_credentials()
    {
        var response = await RequestTokenAsync(IdentityServerTestHost.ClientId, secret: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> TokenAsync(string clientId, string secret)
    {
        var response = await RequestTokenAsync(clientId, secret);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString()!;
    }

    private async Task<HttpResponseMessage> RequestTokenAsync(string clientId, string? secret)
    {
        var parameters = new Dictionary<string, string> { ["grant_type"] = "client_credentials", ["client_id"] = clientId };
        if (secret is not null)
            parameters["client_secret"] = secret;

        using var form = new FormUrlEncodedContent(parameters);
        return await _host.CreateClient().PostAsync("/connect/token", form);
    }

    private async Task<HttpResponseMessage> CallProbeAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/probe");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _host.CreateClient().SendAsync(request);
    }
}
