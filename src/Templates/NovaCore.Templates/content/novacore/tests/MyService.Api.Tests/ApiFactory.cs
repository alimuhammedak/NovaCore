using System.Collections.Concurrent;
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
using Microsoft.IdentityModel.JsonWebTokens;
using MyService.Modules.Todos.Infrastructure;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.EntityFrameworkCore.Messaging;
using NovaCore.EntityFrameworkCore.Persistence;
#if (BuiltInAuth)
using NovaCore.Identity.OpenIddict;
#endif

namespace MyService.Api.Tests;

/// <summary>
/// Starts the real host (Program.cs) in memory, in Development, with startup migrations off. JWT validation points at an
/// in-memory OpenID Connect issuer (<see cref="IdentityProvider"/>), and every DbContext runs on its own in-memory SQLite
/// database, so the tests need no SQL Server and no network. Permissions are resolved on the server, never read from the
/// token: <see cref="CreateClientWithPermissions"/> grants them to a fresh subject.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    // A named in-memory database per context; each DbContext opens its own connection to it, because the outbox
    // dispatcher reads in the background while requests write, and a SqliteConnection isn't thread-safe. These open
    // connections keep the databases alive as long as the factory.
    private readonly SqliteConnection _todosDatabase = OpenSharedInMemoryDatabase();
#if (BuiltInAuth)
    private readonly SqliteConnection _identityDatabase = OpenSharedInMemoryDatabase();
#endif

    // The SQLite contexts get their own EF Core internal service provider: the host registers SQL Server, and EF Core
    // allows one provider per container.
    private readonly ServiceProvider _sqliteServices = new ServiceCollection()
        .AddEntityFrameworkSqlite()
        .BuildServiceProvider();

    private readonly ConcurrentDictionary<string, string[]> _grants = new();

    public ApiFactory()
    {
        // The schemas exist from the start, as after the migrations: the host's background services (the outbox
        // dispatcher, the identity provider's pruning) read them as soon as it starts.
        using var todos = new TodosDbContext(
            new DbContextOptionsBuilder<TodosDbContext>().UseSqlite(_todosDatabase).Options);
        todos.Database.EnsureCreated();
#if (BuiltInAuth)

        using var identity = new IdentityServerDbContext(
            new DbContextOptionsBuilder<IdentityServerDbContext>().UseSqlite(_identityDatabase).Options);
        identity.Database.EnsureCreated();
#endif
    }

    /// <summary>
    /// The issuer the host trusts through <c>Jwt:Authority</c>. Its tokens carry the real time: JWT validation reads the
    /// real clock, not the host's <see cref="TimeProvider"/>.
    /// </summary>
    public TestIdentityProvider IdentityProvider { get; } = new(TimeProvider.System);

    /// <summary>A client whose bearer token carries exactly <paramref name="claims"/>.</summary>
    public HttpClient CreateClientWithClaims(params Claim[] claims)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IdentityProvider.CreateToken(claims));
        return client;
    }

    /// <summary>A client for a fresh subject that holds exactly <paramref name="permissions"/>.</summary>
    public HttpClient CreateClientWithPermissions(params string[] permissions)
    {
        var subject = Guid.NewGuid().ToString();
        _grants[subject] = permissions;
        return CreateClientWithClaims(new Claim(NovaClaims.Subject, subject));
    }

    // Captive dependencies and scoped services resolved from the root fail the host build in every environment.
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
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Authority"] = TestIdentityProvider.Issuer,
                ["Database:ApplyMigrations"] = "false",
#if (BuiltInAuth)
                ["Identity:Seed:SeedOnStartup"] = "false",
#endif
                // Keep the rate limiter wired but out of the tests' way.
                ["RateLimiting:PermitLimit"] = "100000"
            }));

        builder.ConfigureServices(services =>
        {
            // Configure, not PostConfigure: JwtBearer builds its metadata backchannel in PostConfigure.
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,
                options => options.BackchannelHttpHandler = IdentityProvider.Handler);

#if (BuiltInAuth)
            // The test issuer's subjects hold what CreateClientWithPermissions granted; the identity provider's own
            // tokens go to the host's resolver, which reads its store.
            var hostResolver = services.Single(d => d.ServiceType == typeof(IPermissionResolver));
            services.Remove(hostResolver);
            services.AddScoped<IPermissionResolver>(provider => new GrantingPermissionResolver(
                _grants, (IPermissionResolver)ActivatorUtilities.CreateInstance(provider, hostResolver.ImplementationType!)));

            ReplaceWithSqlite<IdentityServerDbContext>(services, _identityDatabase.ConnectionString);
#else
            // The test issuer's subjects hold what CreateClientWithPermissions granted. Registered last, this resolver is
            // the one the host uses, also once the project registers its own.
            services.AddScoped<IPermissionResolver>(_ => new GrantingPermissionResolver(_grants, fallback: null));
#endif
            ReplaceWithSqlite<TodosDbContext>(services, _todosDatabase.ConnectionString);
        });
    }

    /// <summary>
    /// Replaces the SQL Server registration of <typeparamref name="TContext"/> with SQLite, keeping the audit and outbox
    /// interceptors. EF Core registers the provider through several services generic over the context, so all of them
    /// are removed first; leaving one behind configures two providers.
    /// </summary>
    private void ReplaceWithSqlite<TContext>(IServiceCollection services, string connectionString)
        where TContext : DbContext
    {
        var registrations = services
            .Where(d => d.ServiceType == typeof(TContext)
                        || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(TContext))))
            .ToList();
        foreach (var registration in registrations)
            services.Remove(registration);

        services.AddDbContext<TContext>((provider, options) => options
            .UseSqlite(connectionString)
            .UseInternalServiceProvider(_sqliteServices)
            .AddInterceptors(
                provider.GetRequiredService<AuditableEntityInterceptor>(),
                provider.GetRequiredService<OutboxInterceptor>()));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _todosDatabase.Dispose();
#if (BuiltInAuth)
            _identityDatabase.Dispose();
#endif
            _sqliteServices.Dispose();
            IdentityProvider.Dispose();
        }

        base.Dispose(disposing);
    }

    // No pooling: a pooled connection would keep the database alive after the factory closes this one.
    private static SqliteConnection OpenSharedInMemoryDatabase()
    {
        var connection = new SqliteConnection($"Data Source=tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False");
        connection.Open();
        return connection;
    }

    private sealed class GrantingPermissionResolver(
        IReadOnlyDictionary<string, string[]> grants, IPermissionResolver? fallback) : IPermissionResolver
    {
        public Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
        {
            if (fallback is not null && user.FindFirstValue(JwtRegisteredClaimNames.Iss) != TestIdentityProvider.Issuer)
                return fallback.GetPermissionsAsync(user, cancellationToken);

            return Task.FromResult<IReadOnlyCollection<string>>(
                user.FindFirstValue(NovaClaims.Subject) is { } subject && grants.TryGetValue(subject, out var granted)
                    ? granted
                    : []);
        }
    }
}
