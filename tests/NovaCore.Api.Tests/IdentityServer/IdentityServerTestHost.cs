using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.AspNetCore.Web;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// A host composed the way a consumer of the NovaCore.Identity.OpenIddict package composes one: the core packages plus the
/// add-on, no modules and no Api project. The store is an in-memory SQLite database. The server runs at
/// <see cref="Authority"/>, which is also <c>Jwt:Authority</c>, so the core validates the add-on's tokens through this
/// server's own discovery document and keys, over the in-memory transport.
/// </summary>
public sealed class IdentityServerTestHost : IAsyncDisposable
{
    /// <summary>The TestServer's base address, the issuer and <c>Jwt:Authority</c>.</summary>
    public const string Authority = "https://localhost";

    /// <summary>The configured public client that signs users in.</summary>
    public const string ClientId = "spa";

    public const string RedirectUri = "https://client.novacore.test/callback";

    public const string PostLogoutRedirectUri = "https://client.novacore.test/signed-out";

    /// <summary>What <c>GET /probe</c> requires: the core's permission check on a token from this provider.</summary>
    public const string ProbePermission = "probe:read";

    private readonly WebApplication _app;
    private readonly SqliteConnection _connection;

    private IdentityServerTestHost(WebApplication app, SqliteConnection connection)
    {
        _app = app;
        _connection = connection;
    }

    public IServiceProvider Services => _app.Services;

    public TestServer Server => _app.GetTestServer();

    /// <summary>
    /// Builds and starts the host. <paramref name="configure"/> runs after the standard registrations, so it can add
    /// configuration (later sources win) or services, e.g. the certificates a host adds outside Development.
    /// </summary>
    public static async Task<IdentityServerTestHost> StartAsync(
        string environment = "Development", Action<WebApplicationBuilder>? configure = null)
    {
        // A named in-memory database that lives while this connection is open. Each context instance opens its own
        // connection to it: the token pruner uses the store in the background while requests do, and a SqliteConnection
        // isn't thread-safe (docs/plan/RESEARCH.md R-069). No pooling, so the database goes when the host does.
        var connection = new SqliteConnection(
            $"Data Source=identity-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False");
        connection.Open();
        WebApplication? app = null;

        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });

            // This host's own configuration only. The test project's output also holds the Api project's appsettings
            // files, which a host of the core and the add-on wouldn't have: their Identity section names the Api's
            // modules and clients. The options' defaults stand in for the rest.
            foreach (var file in builder.Configuration.Sources.OfType<JsonConfigurationSource>().ToList())
                builder.Configuration.Sources.Remove(file);

            builder.Logging.ClearProviders();
            builder.WebHost.UseTestServer(options => options.BaseAddress = new Uri(Authority));
            builder.Host.UseDefaultServiceProvider(options =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            });

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Authority"] = Authority,
                ["Database:ApplyMigrations"] = "false",
                ["RateLimiting:PermitLimit"] = "100000",
                ["Identity:Seed:SeedOnStartup"] = "true",
                ["Identity:Clients:0:ClientId"] = ClientId,
                ["Identity:Clients:0:RedirectUris:0"] = RedirectUri,
                ["Identity:Clients:0:PostLogoutRedirectUris:0"] = PostLogoutRedirectUri
            });

            builder.AddCoreWeb();
            builder.AddCorePersistence();
            builder.AddCoreIdentityServer();
            builder.Services.AddModuleDbContext<IdentityServerDbContext>((provider, db) => db
                .UseSqlite(connection.ConnectionString)
                .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>()));

            // JwtBearer reads the authority's metadata and keys from this very server (docs/plan/RESEARCH.md R-056).
            builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                options => options.BackchannelHttpHandler = new TestServerHandler(() => app!.GetTestServer()));

            configure?.Invoke(builder);

            app = builder.Build();
            app.UseCoreWeb();
            app.MapCoreIdentityServer();
            app.MapGet("/probe", () => "ok").RequirePermission(ProbePermission);

            await using (var scope = app.Services.CreateAsyncScope())
                await scope.ServiceProvider.GetRequiredService<IdentityServerDbContext>().Database.EnsureCreatedAsync();

            await app.StartAsync();
            return new IdentityServerTestHost(app, connection);
        }
        catch
        {
            if (app is not null)
                await app.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }
    }

    public HttpClient CreateClient() => Server.CreateClient();

    /// <summary>A confirmed user with <paramref name="password"/>, holding <paramref name="permissions"/> as direct grants.</summary>
    public async Task<ApplicationUser> CreateUserAsync(string email, string password, params string[] permissions)
    {
        await using var scope = Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            CreatedAtUtc = TimeProvider.System.GetUtcNow()
        };
        var created = await users.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Description)));

        foreach (var permission in permissions)
            await users.AddClaimAsync(user, new Claim(NovaClaims.Permission, permission));

        return user;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        await _connection.DisposeAsync();
    }

    // The backchannel is configured before the server exists, so it resolves the server's handler on first use.
    private sealed class TestServerHandler(Func<TestServer> server) : HttpMessageHandler
    {
        private HttpMessageInvoker? _invoker;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            (_invoker ??= new HttpMessageInvoker(server().CreateHandler())).SendAsync(request, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _invoker?.Dispose();
            base.Dispose(disposing);
        }
    }
}

/// <summary>
/// One <see cref="IdentityServerTestHost"/> shared by a test class (<c>IClassFixture</c>): tests that don't need a host of
/// their own use unique accounts instead. Override <see cref="Configure"/> to change the host.
/// </summary>
public class IdentityServerFixture : IAsyncLifetime
{
    public IdentityServerTestHost Host { get; private set; } = null!;

    public async ValueTask InitializeAsync() => Host = await IdentityServerTestHost.StartAsync(configure: Configure);

    public async ValueTask DisposeAsync()
    {
        await Host.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected virtual void Configure(WebApplicationBuilder builder)
    {
    }
}
