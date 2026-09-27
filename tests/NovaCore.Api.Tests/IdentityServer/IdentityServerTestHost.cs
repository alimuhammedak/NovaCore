using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NovaCore.AspNetCore.Web;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Identity.OpenIddict;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// A host composed the way a consumer of the NovaCore.Identity.OpenIddict package composes one: the core packages plus the
/// add-on, no modules and no Api project. The store is an in-memory SQLite database. The server runs at
/// <see cref="Authority"/>, which is also <c>Jwt:Authority</c>, so the core validates the add-on's tokens through this
/// server's own discovery document and keys, over the in-memory transport.
/// </summary>
internal sealed class IdentityServerTestHost : IAsyncDisposable
{
    /// <summary>The TestServer's base address, the issuer and <c>Jwt:Authority</c>.</summary>
    public const string Authority = "https://localhost";

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
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        WebApplication? app = null;

        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
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
                ["RateLimiting:PermitLimit"] = "100000"
            });

            builder.AddCoreWeb();
            builder.AddCorePersistence();
            builder.AddCoreIdentityServer();
            builder.Services.AddModuleDbContext<IdentityServerDbContext>((provider, db) => db
                .UseSqlite(connection)
                .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>()));

            // JwtBearer reads the authority's metadata and keys from this very server (docs/plan/RESEARCH.md R-056).
            builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                options => options.BackchannelHttpHandler = new TestServerHandler(() => app!.GetTestServer()));

            configure?.Invoke(builder);

            app = builder.Build();
            app.UseCoreWeb();

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
