using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// How NovaCore.Identity.OpenIddict configures the OpenID Connect server (PLAN F3b.1, DECISIONS D-026): the issuer is
/// <c>Jwt:Authority</c>, PKCE is required, keys are ephemeral in Development only and a host elsewhere must bring its
/// own, RSA or EC.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
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
    public async Task The_OpenAPI_document_declares_the_sign_in_flow()
    {
        await using var host = await IdentityServerTestHost.StartAsync();

        var document = await host.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var scheme = document.GetProperty("components").GetProperty("securitySchemes").GetProperty("OAuth2");
        Assert.Equal("oauth2", scheme.GetProperty("type").GetString());
        var flow = scheme.GetProperty("flows").GetProperty("authorizationCode");
        Assert.Equal(IdentityServerTestHost.Authority + "/connect/authorize", flow.GetProperty("authorizationUrl").GetString());
        Assert.Equal(IdentityServerTestHost.Authority + "/connect/token", flow.GetProperty("tokenUrl").GetString());
        Assert.True(flow.GetProperty("scopes").TryGetProperty("openid", out _));
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
        // With a Data Protection key store, so the missing signing keys are what stops the host.
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => IdentityServerTestHost.StartAsync(
            Environments.Production, builder => InMemoryKeyStore.AddTo(builder.Services)));

        Assert.Contains("key must be registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Outside_Development_the_host_needs_a_data_protection_key_store()
    {
        // Cookies, antiforgery tokens and confirmation links are protected with these keys: without a store they would
        // change on every restart and differ between instances (PLAN F4c.6).
        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => IdentityServerTestHost.StartAsync(Environments.Production, builder => AddProductionServices(builder, keyStore: false)));

        Assert.Contains("Data Protection key store", exception.Message, StringComparison.Ordinal);

        await using var host = await IdentityServerTestHost.StartAsync(
            Environments.Production, builder => AddProductionServices(builder, keyStore: true));
        Assert.IsType<InMemoryKeyStore>(host.Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository);
    }

    // Microsoft's antiforgery guidance (R-104). Development keeps plain-http pages working.
    [Fact]
    public async Task Outside_Development_the_antiforgery_cookie_is_always_secure()
    {
        await using var host = await IdentityServerTestHost.StartAsync(
            Environments.Production, builder => AddProductionServices(builder, keyStore: true));

        Assert.Equal(CookieSecurePolicy.Always, host.Services.GetRequiredService<IOptions<AntiforgeryOptions>>().Value.Cookie.SecurePolicy);
    }

    [Fact]
    public async Task In_Development_the_framework_default_key_store_is_enough()
    {
        await using var host = await IdentityServerTestHost.StartAsync();

        Assert.Null(host.Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository);
    }

    [Fact]
    public async Task A_host_outside_Development_can_sign_with_an_EC_key()
    {
        await using var host = await IdentityServerTestHost.StartAsync(Environments.Production, builder =>
        {
            builder.Services.AddOpenIddict().AddServer(server => server
                .AddEphemeralSigningKey(SecurityAlgorithms.EcdsaSha256)
                .AddEphemeralEncryptionKey());
            builder.Services.AddSingleton<IEmailSender>(new CapturingEmailSender());
            InMemoryKeyStore.AddTo(builder.Services);
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:Clients:1:ClientId"] = "worker",
                ["Identity:Clients:1:ClientSecret"] = "worker-secret-for-tests-at-least-32-chars",
                ["Identity:Clients:1:Permissions:0"] = IdentityServerTestHost.ProbePermission
            });
        });

        var key = Assert.Single(await SigningKeysAsync(host));
        Assert.Equal("EC", key.GetProperty("kty").GetString());
        Assert.Equal(SecurityAlgorithms.EcdsaSha256, key.GetProperty("alg").GetString());

        // The core accepts the ES256 token through the published key.
        var client = host.CreateClient();
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "worker",
            ["client_secret"] = "worker-secret-for-tests-at-least-32-chars"
        });
        var token = await client.PostAsync("/connect/token", form);
        var accessToken = (await token.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString()!;
        Assert.Equal(SecurityAlgorithms.EcdsaSha256, new JsonWebToken(accessToken).Alg);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/probe")).StatusCode);
    }

    [Theory]
    [InlineData("ClientId", "0199a3c5-0000-7000-8000-000000000000", "must not be a Guid")]
    [InlineData("RedirectUris:0", "callback", "must be absolute URIs")]
    [InlineData("Permissions:0", "probe:read", "apply to service clients only")]
    public async Task An_invalid_client_stops_the_host(string key, string value, string expectedInMessage)
    {
        // Changes the host's user-facing client (Identity:Clients:0).
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => IdentityServerTestHost.StartAsync(
            configure: builder => builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Identity:Clients:0:" + key] = value })));

        Assert.Contains(expectedInMessage, exception.Message, StringComparison.Ordinal);
    }

    // RFC 8252 §7.3: outside Development, plain http is only a native app's loopback listener, named by its IP address
    // ("localhost" can resolve elsewhere, §8.3). Anything else would send the code over the network unencrypted.
    [Theory]
    [InlineData("RedirectUris:0", "http://client.novacore.test/callback", false)]
    [InlineData("RedirectUris:0", "http://localhost:5000/callback", false)]
    [InlineData("PostLogoutRedirectUris:0", "http://client.novacore.test/signed-out", false)]
    [InlineData("RedirectUris:0", "http://127.0.0.1:5000/callback", true)]
    [InlineData("RedirectUris:0", "http://[::1]:5000/callback", true)]
    public async Task Outside_Development_an_http_redirect_must_be_a_loopback_address(string key, string uri, bool starts)
    {
        Task<IdentityServerTestHost> StartAsync() => IdentityServerTestHost.StartAsync(Environments.Production, builder =>
        {
            AddProductionServices(builder, keyStore: true);
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Identity:Clients:0:" + key] = uri });
        });

        if (starts)
        {
            await using var host = await StartAsync();
            return;
        }

        var exception = await Assert.ThrowsAnyAsync<Exception>(StartAsync);
        Assert.Contains("must use https outside Development", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task In_Development_an_http_redirect_is_allowed()
    {
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
            builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Identity:Clients:0:RedirectUris:0"] = "http://client.novacore.test/callback" }));
    }

    // RFC 6749 §10.10: a secret that can be guessed is no secret; 32 characters hold 128 bits or more when generated.
    [Theory]
    [InlineData("1", "worker", "a-secret-of-31-characters-xxxxx")]
    [InlineData("0", IdentityServerTestHost.ClientId, "short")]
    public async Task A_client_secret_shorter_than_32_characters_stops_the_host(string index, string clientId, string secret)
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => IdentityServerTestHost.StartAsync(
            configure: builder => builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Identity:Clients:{index}:ClientId"] = clientId,
                [$"Identity:Clients:{index}:ClientSecret"] = secret
            })));

        Assert.Contains("ClientSecret must be at least 32 characters", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_service_client_without_a_secret_stops_the_host()
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => IdentityServerTestHost.StartAsync(
            configure: builder => builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Identity:Clients:1:ClientId"] = "worker" })));

        Assert.Contains("needs a ClientSecret", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("nobody:declares:this", "must list permissions a module declares")]
    [InlineData("identity:users:manage", "can't hold the identity provider's own permissions")]
    public async Task A_User_role_permission_outside_the_modules_declarations_stops_the_host(string permission, string expectedInMessage)
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => IdentityServerTestHost.StartAsync(
            configure: builder => builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Identity:Seed:UserPermissions:0"] = permission })));

        Assert.Contains(expectedInMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_host_does_not_start_without_an_authority()
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => IdentityServerTestHost.StartAsync(
            configure: builder => builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Jwt:Authority"] = "" })));

        // The core's own rule: the provider can't pick an issuer without it.
        Assert.Contains("Jwt:Authority must be the absolute URI of an OpenID Connect provider", exception.Message, StringComparison.Ordinal);
    }

    // What a production host brings besides the configuration: certificates (ephemeral keys here), an email sender and,
    // unless a test leaves it out, a Data Protection key store.
    private static void AddProductionServices(WebApplicationBuilder builder, bool keyStore)
    {
        builder.Services.AddOpenIddict().AddServer(server => server.AddEphemeralSigningKey().AddEphemeralEncryptionKey());
        builder.Services.AddSingleton<IEmailSender>(new CapturingEmailSender());
        if (keyStore)
            InMemoryKeyStore.AddTo(builder.Services);
    }

    private static async Task<List<JsonElement>> SigningKeysAsync(IdentityServerTestHost host)
    {
        var jwks = await host.CreateClient().GetFromJsonAsync<JsonElement>("/.well-known/jwks");
        return jwks.GetProperty("keys").EnumerateArray().ToList();
    }

    private static string[] Strings(JsonElement document, string property) =>
        document.GetProperty(property).EnumerateArray().Select(value => value.GetString()!).ToArray();
}
