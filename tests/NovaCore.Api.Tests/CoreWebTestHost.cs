using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Web;

namespace NovaCore.Api.Tests;

/// <summary>
/// A host composed the way a consumer of the core web package composes one: <c>AddCoreWeb</c>, a permission resolver and
/// <c>UseCoreWeb</c>, without modules, persistence or an identity provider. Each test maps the endpoints it needs, which
/// the Api host can't take: an endpoint without authorization metadata, one that echoes the request, one that throws.
/// Access tokens come from <see cref="IdentityProvider"/>, the host's <c>Jwt:Authority</c>.
/// </summary>
public sealed class CoreWebTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private CoreWebTestHost(WebApplication app, TestIdentityProvider identityProvider)
    {
        _app = app;
        IdentityProvider = identityProvider;
    }

    public TestIdentityProvider IdentityProvider { get; }

    public IServiceProvider Services => _app.Services;

    public TestServer Server => _app.GetTestServer();

    /// <summary>
    /// Builds and starts the host. <paramref name="configure"/> runs after the core registrations, so it can add
    /// configuration (later sources win) or services; <paramref name="map"/> maps the test's endpoints after
    /// <c>UseCoreWeb</c>.
    /// </summary>
    public static async Task<CoreWebTestHost> StartAsync(
        Action<WebApplication> map, string environment = "Development", Action<WebApplicationBuilder>? configure = null)
    {
        var identityProvider = new TestIdentityProvider();
        WebApplication? app = null;

        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
            builder.Logging.ClearProviders();
            builder.WebHost.UseTestServer();
            builder.Host.UseDefaultServiceProvider(options =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            });

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Jwt:Authority"] = TestIdentityProvider.Issuer,
                ["RateLimiting:PermitLimit"] = "100000"
            });

            builder.AddCoreWeb();
            builder.Services.AddScoped<IPermissionResolver, NoPermissions>();
            builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                options => options.BackchannelHttpHandler = identityProvider.Handler);

            configure?.Invoke(builder);

            app = builder.Build();
            app.UseCoreWeb();
            map(app);

            await app.StartAsync();
            return new CoreWebTestHost(app, identityProvider);
        }
        catch
        {
            if (app is not null)
                await app.DisposeAsync();
            identityProvider.Dispose();
            throw;
        }
    }

    public HttpClient CreateClient() => Server.CreateClient();

    /// <summary>A client whose bearer token, from <see cref="IdentityProvider"/>, carries <paramref name="claims"/>.</summary>
    public HttpClient CreateClientWithClaims(params Claim[] claims)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IdentityProvider.CreateToken(claims));
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        IdentityProvider.Dispose();
    }

    // Callers hold no permissions: the tests here are about the pipeline, not about permissions.
    private sealed class NoPermissions : IPermissionResolver
    {
        public Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<string>>([]);
    }
}
