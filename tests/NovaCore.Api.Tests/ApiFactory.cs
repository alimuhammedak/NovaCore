using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using NovaCore.Api.Tests.IdentityServer;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.Identity.OpenIddict;

namespace NovaCore.Api.Tests;

/// <summary>
/// Boots the real Program host in-memory (Development), with migrations and seeding off. JWT validation points at an
/// in-memory OpenID Connect issuer (<see cref="IdentityProvider"/>), and permissions are granted on the server. Outside
/// Development the host gets what a production host must bring: signing and encryption keys for the identity provider, an
/// email sender and a Data Protection key store. A subclass chooses the databases: <see cref="NovaCoreApiFactory"/>
/// in-memory SQLite, <c>SqlServerApiFactory</c> a real SQL Server.
/// </summary>
public abstract class ApiFactory : WebApplicationFactory<Program>
{
    // Permissions granted on the server to subjects of IdentityProvider's tokens (none unless granted). Other tokens, the
    // identity provider add-on's, are resolved by the host's resolver from its store.
    private readonly ConcurrentDictionary<string, string[]> _grants = new();

    /// <summary>The host environment the app boots in. Override to test environment-specific wiring.</summary>
    protected virtual string EnvironmentName => Environments.Development;

    /// <summary>The issuer the host trusts through <c>Jwt:Authority</c>, in every environment.</summary>
    public TestIdentityProvider IdentityProvider { get; } = new();

    /// <summary>
    /// A client whose bearer token carries exactly <paramref name="claims"/>, issued by <see cref="IdentityProvider"/>
    /// (RS256, validated through the host's <c>Jwt:Authority</c>): exercises authorization and validation without
    /// seeding users.
    /// </summary>
    public HttpClient CreateClientWithClaims(params Claim[] claims)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IdentityProvider.CreateToken(claims));
        return client;
    }

    /// <summary>
    /// A client for a fresh subject holding exactly <paramref name="permissions"/>. The core takes permissions from
    /// <see cref="IPermissionResolver"/>, never from the token (PLAN F3a.2), so they are granted on the server; any other
    /// subject of <see cref="IdentityProvider"/> holds none.
    /// </summary>
    public HttpClient CreateClientWithPermissions(params string[] permissions)
    {
        var subject = Guid.NewGuid().ToString();
        GrantPermissions(subject, permissions);
        return CreateClientWithClaims(new Claim(NovaClaims.Subject, subject));
    }

    /// <summary>Sets the permissions the server resolves for <paramref name="subject"/>, replacing earlier grants.</summary>
    public void GrantPermissions(string subject, params string[] permissions) => _grants[subject] = permissions;

    // DECISIONS Y-26: captive dependencies and root-resolved scoped services fail the host build in every environment,
    // not only in Development where they are on by default (R-029).
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Authority"] = TestIdentityProvider.Issuer,
                ["Database:ApplyMigrations"] = "false",
                ["Identity:Seed:SeedOnStartup"] = "false",
                // Keep the rate limiter wired but out of the way of the many auth calls the tests make.
                ["RateLimiting:PermitLimit"] = "100000"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Configure (not PostConfigure): JwtBearer builds its metadata backchannel in PostConfigure (R-056).
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                options => options.BackchannelHttpHandler = IdentityProvider.Handler);

            // IdentityProvider's subjects resolve to their grants; the add-on's tokens to the host's resolver.
            var hostResolver = services.Single(d => d.ServiceType == typeof(IPermissionResolver));
            services.Remove(hostResolver);
            services.AddScoped<IPermissionResolver>(provider => new GrantingPermissionResolver(
                _grants, (IPermissionResolver)ActivatorUtilities.CreateInstance(provider, hostResolver.ImplementationType!)));

            // A production host brings keys (certificates, stood in for by ephemeral keys), a real email sender and a
            // Data Protection key store.
            if (EnvironmentName != Environments.Development)
            {
                services.AddOpenIddict().AddServer(server => server.AddEphemeralSigningKey().AddEphemeralEncryptionKey());
                services.AddSingleton<IEmailSender>(new CapturingEmailSender());
                InMemoryKeyStore.AddTo(services);
            }

            ConfigureDatabases(services);
        });
    }

    /// <summary>Points the host's DbContexts (the modules' and the identity provider's) at the test's databases.</summary>
    protected abstract void ConfigureDatabases(IServiceCollection services);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            IdentityProvider.Dispose();

        base.Dispose(disposing);
    }

    private sealed class GrantingPermissionResolver(
        IReadOnlyDictionary<string, string[]> grants, IPermissionResolver hostResolver) : IPermissionResolver
    {
        public Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            if (user.FindFirstValue(JwtRegisteredClaimNames.Iss) != TestIdentityProvider.Issuer)
                return hostResolver.GetPermissionsAsync(user, cancellationToken);

            return Task.FromResult<IReadOnlyCollection<string>>(
                user.FindFirstValue(NovaClaims.Subject) is { } subject && grants.TryGetValue(subject, out var granted)
                    ? granted
                    : []);
        }
    }
}
