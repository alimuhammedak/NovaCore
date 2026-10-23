using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MyService.Api.Tests;

/// <summary>
/// The host runs its own OpenID Connect provider (NovaCore.Identity.OpenIddict). Its flows are tested in the package; this
/// checks that Program.cs wires it: the provider publishes its discovery document, with Jwt:Authority as the issuer.
/// </summary>
public sealed class IdentityProviderTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task The_provider_publishes_its_discovery_document()
    {
        // The provider answers over HTTPS only.
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        var discovery = await client.GetFromJsonAsync<JsonElement>(
            new Uri("/.well-known/openid-configuration", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(TestIdentityProvider.Issuer + "/", discovery.GetProperty("issuer").GetString());
        Assert.True(discovery.TryGetProperty("authorization_endpoint", out _));
        Assert.True(discovery.TryGetProperty("token_endpoint", out _));
    }
}
