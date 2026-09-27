using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Identity.Infrastructure;
using NovaCore.Modules.Payments.Infrastructure;

namespace NovaCore.Api.Tests;

/// <summary>
/// Boots the real Program host in-memory (Development), with migrations off. JWT validation points at an in-memory
/// OpenID Connect issuer (<see cref="IdentityProvider"/>); in Development the Identity module's own HS256 tokens are
/// also accepted, through a fixed signing key. Every module's DbContext is swapped to its own in-memory SQLite database,
/// so the full auth flow (register/login/refresh/permissions) and the database health checks run without a real SQL Server.
/// </summary>
public class NovaCoreApiFactory : WebApplicationFactory<Program>
{
    // One open connection per module keeps each in-memory database alive for the factory's lifetime.
    private readonly SqliteConnection _identityConnection = new("DataSource=:memory:");
    private readonly SqliteConnection _catalogConnection = new("DataSource=:memory:");
    private readonly SqliteConnection _paymentsConnection = new("DataSource=:memory:");

    // The SQLite contexts get their own EF internal provider: the app still registers SQL Server, and EF forbids two
    // providers in one container.
    private readonly ServiceProvider _sqliteServices = new ServiceCollection()
        .AddEntityFrameworkSqlite()
        .BuildServiceProvider();

    public NovaCoreApiFactory()
    {
        _identityConnection.Open();
        _catalogConnection.Open();
        _paymentsConnection.Open();
    }

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
                // The symmetric key is Development-only (D-024): elsewhere it stops the host.
                ["Jwt:SigningKey"] = EnvironmentName == Environments.Development
                    ? "novacore-integration-test-signing-key-0123456789-abcdef"
                    : null,
                ["Jwt:AccessTokenMinutes"] = "15",
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

            ReplaceWithSqlite<IdentityDbContext>(services, options => options.UseSqlite(_identityConnection));
            ReplaceWithSqlite<CatalogDbContext>(services, options => options.UseSqlite(_catalogConnection));
            ReplaceWithSqlite<PaymentsDbContext>(services, options => options.UseSqlite(_paymentsConnection));
        });
    }

    /// <summary>
    /// Replaces a module's SQL Server registration of <typeparamref name="TContext"/> with SQLite, keeping the audit
    /// interceptor the module adds. EF Core 10 registers the provider via additive
    /// <c>IDbContextOptionsConfiguration&lt;TContext&gt;</c>, so EVERY registration generic over the context (options,
    /// configuration, the context) is removed first.
    /// </summary>
    protected void ReplaceWithSqlite<TContext>(IServiceCollection services, Action<DbContextOptionsBuilder> useSqlite)
        where TContext : DbContext
    {
        var toRemove = services
            .Where(d => d.ServiceType == typeof(TContext)
                        || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(TContext))))
            .ToList();
        foreach (var descriptor in toRemove)
            services.Remove(descriptor);

        services.AddDbContext<TContext>((provider, options) =>
        {
            useSqlite(options);
            options
                .UseInternalServiceProvider(_sqliteServices)
                .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>());
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _identityConnection.Dispose();
            _catalogConnection.Dispose();
            _paymentsConnection.Dispose();
            _sqliteServices.Dispose();
            IdentityProvider.Dispose();
        }

        base.Dispose(disposing);
    }
}
