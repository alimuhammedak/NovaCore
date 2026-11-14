using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NovaCore.Identity.OpenIddict;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The seed brings the configured clients up to date on every start. A client whose configuration didn't change keeps
/// its stored secret hash (no new PBKDF2 hash, no write); a changed secret replaces it (rotation), and a change to the
/// client's other fields is still written.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class ClientSeedTests(ClientSeedTests.Fixture fixture) : IClassFixture<ClientSeedTests.Fixture>
{
    private const string WorkerSecret = "worker-secret-for-tests-at-least-32-chars";
    private const string ReporterSecret = "reporter-secret-for-tests-at-least-32-chars";

    private readonly IdentityServerTestHost _host = fixture.Host;

    /// <summary>Two service clients: each test seeds again with its own change to one of them.</summary>
    public sealed class Fixture : IdentityServerFixture
    {
        protected override void Configure(WebApplicationBuilder builder) =>
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Identity:Clients:1:ClientId"] = "worker",
                ["Identity:Clients:1:ClientSecret"] = WorkerSecret,
                ["Identity:Clients:2:ClientId"] = "reporter",
                ["Identity:Clients:2:ClientSecret"] = ReporterSecret
            });
    }

    [Fact]
    public async Task A_second_seed_with_the_same_configuration_writes_nothing()
    {
        // The other tests change the stored clients: this seed brings them back to the configuration first.
        await SeedAsync(Configured());
        var before = await StoredAsync("worker");

        await SeedAsync(Configured());

        var after = await StoredAsync("worker");
        Assert.Equal(before.ClientSecret, after.ClientSecret);
        Assert.Equal(before.ConcurrencyToken, after.ConcurrencyToken);
        Assert.Equal(HttpStatusCode.OK, (await RequestTokenAsync("worker", WorkerSecret)).StatusCode);
    }

    [Fact]
    public async Task A_change_to_another_field_is_written_without_hashing_the_secret_again()
    {
        var before = await StoredAsync("worker");
        var clients = Configured();
        clients.Single(client => string.Equals(client.ClientId, "worker", StringComparison.Ordinal)).DisplayName = "Worker service";

        await SeedAsync(clients);

        var after = await StoredAsync("worker");
        Assert.Equal("Worker service", after.DisplayName);
        Assert.Equal(before.ClientSecret, after.ClientSecret);
        Assert.Equal(HttpStatusCode.OK, (await RequestTokenAsync("worker", WorkerSecret)).StatusCode);
    }

    [Fact]
    public async Task A_rotated_secret_replaces_the_old_one()
    {
        var clients = Configured();
        clients.Single(client => string.Equals(client.ClientId, "reporter", StringComparison.Ordinal)).ClientSecret = WorkerSecret;

        await SeedAsync(clients);

        Assert.Equal(HttpStatusCode.OK, (await RequestTokenAsync("reporter", WorkerSecret)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RequestTokenAsync("reporter", ReporterSecret)).StatusCode);
    }

    // The host's clients as configured; a test changes its copy only, so the host's options stay as they were.
    private List<IdentityClientOptions> Configured() =>
    [
        .. _host.Services.GetRequiredService<IOptions<IdentityServerOptions>>().Value.Clients.Select(client => new IdentityClientOptions
        {
            ClientId = client.ClientId,
            DisplayName = client.DisplayName,
            ClientSecret = client.ClientSecret,
            RedirectUris = [.. client.RedirectUris],
            PostLogoutRedirectUris = [.. client.PostLogoutRedirectUris],
            Permissions = [.. client.Permissions]
        })
    ];

    // Runs the seed as a later start would, with the given clients.
    private async Task SeedAsync(List<IdentityClientOptions> clients)
    {
        var configured = _host.Services.GetRequiredService<IOptions<IdentityServerOptions>>().Value;
        var options = new IdentityServerOptions { Seed = configured.Seed, Clients = clients };
        var seeder = ActivatorUtilities.CreateInstance<IdentityServerSeeder>(_host.Services, Options.Create(options));

        await seeder.StartAsync(TestContext.Current.CancellationToken);
    }

    // The stored row: its secret hash, and its concurrency token, which OpenIddict's store renews on every update.
    private async Task<Stored> StoredAsync(string clientId)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = Assert.IsType<OpenIddictEntityFrameworkCoreApplication>(
            await applications.FindByClientIdAsync(clientId, TestContext.Current.CancellationToken));

        return new Stored(application.ClientSecret, application.DisplayName, application.ConcurrencyToken);
    }

    private sealed record Stored(string? ClientSecret, string? DisplayName, string? ConcurrencyToken);

    private async Task<HttpResponseMessage> RequestTokenAsync(string clientId, string secret)
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = secret
        });
        return await _host.CreateClient().PostAsync(new Uri("/connect/token", UriKind.Relative), form);
    }
}
