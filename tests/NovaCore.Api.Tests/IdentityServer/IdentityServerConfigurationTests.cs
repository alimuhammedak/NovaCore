using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// How NovaCore.Identity.OpenIddict configures the OpenID Connect server (PLAN F3b.1, DECISIONS D-026): the issuer is
/// <c>Jwt:Authority</c>, PKCE is required, keys are ephemeral in Development only and a host elsewhere must bring its
/// own, RSA or EC.
/// </summary>
public sealed class IdentityServerConfigurationTests
{
    [Fact]
    public async Task Discovery_document_publishes_the_authority_as_issuer_and_requires_PKCE()
    {
        await using var host = await IdentityServerTestHost.StartAsync();

        var discovery = await host.CreateClient().GetFromJsonAsync<JsonElement>("/.well-known/openid-configuration");

        Assert.Equal(IdentityServerTestHost.Authority + "/", discovery.GetProperty("issuer").GetString());
        Assert.Equal(IdentityServerTestHost.Authority + "/connect/authorize", discovery.GetProperty("authorization_endpoint").GetString());
        Assert.Equal(IdentityServerTestHost.Authority + "/connect/token", discovery.GetProperty("token_endpoint").GetString());
        Assert.Equal(["S256"], Strings(discovery, "code_challenge_methods_supported"));
        Assert.Contains("authorization_code", Strings(discovery, "grant_types_supported"));
        Assert.Contains("refresh_token", Strings(discovery, "grant_types_supported"));
    }

    [Fact]
    public async Task The_issuer_is_the_authority_not_the_request_host()
    {
        // Behind a proxy the request host differs from the public URL; tokens must still carry the authority.
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
            builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Jwt:Authority"] = "https://idp.novacore.test" }));

        var discovery = await host.CreateClient().GetFromJsonAsync<JsonElement>("/.well-known/openid-configuration");

        Assert.Equal("https://idp.novacore.test/", discovery.GetProperty("issuer").GetString());
    }

    [Fact]
    public async Task Development_keys_are_RSA()
    {
        await using var host = await IdentityServerTestHost.StartAsync();

        var key = Assert.Single(await SigningKeysAsync(host));

        Assert.Equal("RSA", key.GetProperty("kty").GetString());
        Assert.Equal(SecurityAlgorithms.RsaSha256, key.GetProperty("alg").GetString());
    }

    [Fact]
    public async Task Outside_Development_the_host_does_not_start_without_keys()
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => IdentityServerTestHost.StartAsync(Environments.Production));

        Assert.Contains("key must be registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_host_outside_Development_can_sign_with_an_EC_key()
    {
        await using var host = await IdentityServerTestHost.StartAsync(Environments.Production, builder =>
            builder.Services.AddOpenIddict().AddServer(server => server
                .AddEphemeralSigningKey(SecurityAlgorithms.EcdsaSha256)
                .AddEphemeralEncryptionKey()));

        var key = Assert.Single(await SigningKeysAsync(host));

        Assert.Equal("EC", key.GetProperty("kty").GetString());
        Assert.Equal(SecurityAlgorithms.EcdsaSha256, key.GetProperty("alg").GetString());
    }

    [Fact]
    public async Task The_host_does_not_start_without_an_authority()
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => IdentityServerTestHost.StartAsync(
            configure: builder => builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Jwt:Authority"] = "" })));

        Assert.Contains("Jwt:Authority must be the absolute public URL of this host", exception.Message, StringComparison.Ordinal);
    }

    private static async Task<List<JsonElement>> SigningKeysAsync(IdentityServerTestHost host)
    {
        var jwks = await host.CreateClient().GetFromJsonAsync<JsonElement>("/.well-known/jwks");
        return jwks.GetProperty("keys").EnumerateArray().ToList();
    }

    private static string[] Strings(JsonElement document, string property) =>
        document.GetProperty(property).EnumerateArray().Select(value => value.GetString()!).ToArray();
}
