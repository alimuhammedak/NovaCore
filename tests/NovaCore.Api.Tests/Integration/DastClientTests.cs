using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NovaCore.Api.Tests.Architecture;
using NovaCore.Api.Tests.IdentityServer;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.Identity.OpenIddict;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// The DAST scan's client (PLAN F6e.1) exists only in the DAST run's configuration: <c>eng/dast/dast.env</c>, which the
/// workflow loads into the host's environment with a secret generated for that run. No committed appsettings file holds
/// it, so a host started from them (Production included) never seeds it; a host given the DAST configuration seeds a
/// client-credentials client with the read permissions listed there and nothing else.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class DastClientTests
{
    private const string ClientId = "dast";

    // The run's secret comes from the environment; this is a test value from .betterleaks.toml's allowlist.
    private const string Secret = "worker-secret-for-tests-at-least-32-chars";

    private static readonly string DastEnvironmentFile = Path.Combine(Solution.RootDirectory, "eng", "dast", "dast.env");

    // Read-only access to the endpoints of the OpenAPI document that need a permission (their GETs): enough for the
    // scanner to reach the handlers behind the permission check, no write and no widening of what a read returns.
    private static readonly string[] ExpectedPermissions =
        ["catalog:products:read", "identity:roles:read", "identity:users:read", "payments:read"];

    private static readonly Regex ShellSafeValue = new("^[A-Za-z0-9:._-]+$", RegexOptions.None, TimeSpan.FromSeconds(1));

    [Fact]
    public void No_committed_appsettings_file_holds_the_DAST_client()
    {
        var files = Solution.SourceFiles("appsettings*.json", "src").ToList();
        Assert.Contains(files, path => path.EndsWith(Path.Combine("NovaCore.Api", "appsettings.json"), StringComparison.Ordinal));
        Assert.Contains(files, path => path.EndsWith(Path.Combine("MyService.Api", "appsettings.json"), StringComparison.Ordinal));

        // Read as text: the template's files hold its #if lines, so they are JSON only once a project is generated.
        var holders =
            from path in files
            where ClientIdSetting(ClientId).IsMatch(File.ReadAllText(path))
            select Path.GetRelativePath(Solution.RootDirectory, path);

        Assert.Empty(holders);
        // The pattern finds a client that is there.
        Assert.Contains(files, path => ClientIdSetting("scalar").IsMatch(File.ReadAllText(path)));
    }

    [Fact]
    public void The_DAST_configuration_defines_only_the_client_and_no_secret()
    {
        var settings = DastSettings();

        Assert.Equal(ClientId, Assert.Single(settings, setting => setting.Key.EndsWith(":ClientId", StringComparison.Ordinal)).Value);
        Assert.All(settings.Keys, key => Assert.StartsWith(DastClientSection(settings) + ":", key, StringComparison.Ordinal));
        Assert.DoesNotContain(settings.Keys, key => key.EndsWith(":ClientSecret", StringComparison.Ordinal));
        Assert.DoesNotContain(settings.Keys, key => key.Contains(":RedirectUris", StringComparison.Ordinal));
        // The workflow sources the file in bash: a space or a quote in a value would split or change it there.
        Assert.All(settings.Values, value => Assert.Matches(ShellSafeValue, value));
    }

    [Fact]
    public async Task A_production_host_from_the_committed_configuration_seeds_no_DAST_client()
    {
        await using var factory = new ProductionSeedingApiFactory();
        _ = factory.Server;

        var clients = factory.Services.GetRequiredService<IOptions<IdentityServerOptions>>().Value.Clients;
        Assert.DoesNotContain(clients, client => string.Equals(client.ClientId, ClientId, StringComparison.Ordinal));
        Assert.Null(await FindAsync(factory.Services));
    }

    [Fact]
    public async Task A_host_with_the_DAST_configuration_seeds_a_client_credentials_client()
    {
        await using var factory = new DastApiFactory();
        _ = factory.Server;

        var application = await FindAsync(factory.Services);
        Assert.NotNull(application);
        await using var scope = factory.Services.CreateAsyncScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        Assert.True(await applications.HasClientTypeAsync(application, ClientTypes.Confidential, TestContext.Current.CancellationToken));
        Assert.Equal(
            new[] { Permissions.Endpoints.Token, Permissions.GrantTypes.ClientCredentials }.Order(StringComparer.Ordinal),
            (await applications.GetPermissionsAsync(application, TestContext.Current.CancellationToken)).Order(StringComparer.Ordinal));
        Assert.Empty(await applications.GetRedirectUrisAsync(application, TestContext.Current.CancellationToken));

        // The Development file's client keeps its place: the DAST client is added, not written over it.
        var clients = factory.Services.GetRequiredService<IOptions<IdentityServerOptions>>().Value.Clients;
        Assert.Contains(clients, client => string.Equals(client.ClientId, "scalar", StringComparison.Ordinal));
    }

    [Fact]
    public async Task The_DAST_clients_tokens_hold_the_read_permissions_only()
    {
        await using var factory = new DastApiFactory();
        _ = factory.Server;
        await using var scope = factory.Services.CreateAsyncScope();

        // What its tokens are granted on the server (the sub of a client-credentials token is the client id).
        var resolver = scope.ServiceProvider.GetServices<IPermissionResolver>().OfType<IdentityPermissionResolver>().Single();
        var granted = await resolver.GetPermissionsAsync(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(NovaClaims.Subject, ClientId)], "test")),
            TestContext.Current.CancellationToken);
        Assert.Equal(ExpectedPermissions, granted.Order(StringComparer.Ordinal), StringComparer.Ordinal);

        // Each one is declared by a module and only reads.
        var declared = scope.ServiceProvider.GetServices<IPermissionDefinitionProvider>()
            .SelectMany(provider => provider.GetPermissions())
            .Select(permission => permission.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Subset(declared, granted.ToHashSet(StringComparer.Ordinal));
        Assert.All(granted, permission => Assert.EndsWith(":read", permission, StringComparison.Ordinal));
    }

    private static Regex ClientIdSetting(string clientId) =>
        new($@"""ClientId""\s*:\s*""{Regex.Escape(clientId)}""", RegexOptions.None, TimeSpan.FromSeconds(1));

    private static async Task<object?> FindAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>()
            .FindByClientIdAsync(ClientId, TestContext.Current.CancellationToken);
    }

    // eng/dast/dast.env as the host sees it: KEY=VALUE lines of environment variables, whose "__" the environment variables
    // configuration provider reads as ":". Blank lines and # comments are skipped, as by the shell that sources it.
    private static Dictionary<string, string?> DastSettings() =>
        File.ReadLines(DastEnvironmentFile)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split('=', 2))
            .ToDictionary(pair => pair[0].Replace("__", ":", StringComparison.Ordinal), string? (pair) => pair[1], StringComparer.Ordinal);

    // Identity:Clients:<n>, the DAST client's entry.
    private static string DastClientSection(Dictionary<string, string?> settings)
    {
        var clientId = settings.Single(setting => setting.Key.EndsWith(":ClientId", StringComparison.Ordinal)).Key;
        return clientId[..clientId.LastIndexOf(':')];
    }

    /// <summary>The test host in Production with the committed configuration only, seeding on start.</summary>
    private sealed class ProductionSeedingApiFactory : NovaCoreApiFactory
    {
        protected override string EnvironmentName => Environments.Production;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) { ["Identity:Seed:SeedOnStartup"] = "true" }));
        }
    }

    /// <summary>The Development test host with the DAST run's configuration and secret, seeding on start.</summary>
    private sealed class DastApiFactory : NovaCoreApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            var settings = DastSettings();
            settings[DastClientSection(settings) + ":ClientSecret"] = Secret;
            settings["Identity:Seed:SeedOnStartup"] = "true";
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
        }
    }
}
