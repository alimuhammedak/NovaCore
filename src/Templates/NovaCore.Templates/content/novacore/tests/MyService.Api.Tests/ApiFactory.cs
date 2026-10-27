using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyService.Modules.Todos.Infrastructure;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.EntityFrameworkCore.Persistence;
#if (BuiltInAuth)
using NovaCore.Identity.OpenIddict;
#endif
#if (multitenancy)
using NovaCore.MultiTenancy;
#endif

namespace MyService.Api.Tests;

/// <summary>
/// Starts the real host (Program.cs) in memory, in Development, with startup migrations off. JWT validation points at an
/// in-memory OpenID Connect issuer (<see cref="IdentityProvider"/>), and every DbContext runs on its own in-memory SQLite
/// database, so the tests need no database server and no network. Permissions are resolved on the server, never read from the
/// token: <see cref="CreateClientWithPermissions"/> grants them to a fresh subject.
#if (multitenancy)
/// A tenant's endpoints also need the caller to be a member of that tenant: <see cref="CreateMemberClientAsync"/> makes a
/// fresh subject a member, whose role there grants the permissions.
#endif
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
#if (multitenancy)
    private readonly SqliteConnection _tenancyDatabase = OpenSharedInMemoryDatabase();
#endif

    private readonly ConcurrentDictionary<string, string[]> _grants = new();

    public ApiFactory()
    {
        // The schemas exist from the start, as after the migrations: the host's background services (the outbox
        // dispatcher, the identity provider's pruning) read them as soon as it starts.
        using var todos = new TodosDbContext(new DbContextOptionsBuilder<TodosDbContext>()
            .UseSqlite(_todosDatabase).ReplaceService<IModelCustomizer, UtcTicksModelCustomizer>().Options);
        todos.Database.EnsureCreated();
#if (BuiltInAuth)

        using var identity = new IdentityServerDbContext(new DbContextOptionsBuilder<IdentityServerDbContext>()
            .UseSqlite(_identityDatabase).ReplaceService<IModelCustomizer, UtcTicksModelCustomizer>().Options);
        identity.Database.EnsureCreated();
#endif
#if (multitenancy)

        using var tenancy = new MultiTenancyDbContext(new DbContextOptionsBuilder<MultiTenancyDbContext>()
            .UseSqlite(_tenancyDatabase).ReplaceService<IModelCustomizer, UtcTicksModelCustomizer>().Options);
        tenancy.Database.EnsureCreated();
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
#if (multitenancy)

    /// <summary>Opens a tenant through the host's <see cref="TenantDirectory"/>.</summary>
    public async Task<Guid> CreateTenantAsync(CancellationToken cancellationToken)
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<TenantDirectory>().CreateAsync("Tenant", cancellationToken);
    }

    /// <summary>
    /// A client for a fresh subject that is a member of each of <paramref name="tenants"/> with <paramref name="role"/>, a
    /// role of MultiTenancy:Roles (appsettings.json): what the role grants is all the subject holds, in those tenants only.
    /// </summary>
    public async Task<HttpClient> CreateMemberClientAsync(string role, IEnumerable<Guid> tenants, CancellationToken cancellationToken)
    {
        var subject = Guid.NewGuid().ToString();
        await using (var scope = Services.CreateAsyncScope())
        {
            var directory = scope.ServiceProvider.GetRequiredService<TenantDirectory>();
            foreach (var tenant in tenants)
            {
                var added = await directory.AddMemberAsync(tenant, subject, [role], cancellationToken);
                if (added.IsFailure)
                    throw new InvalidOperationException(added.Error.Message);
            }
        }

        return CreateClientWithClaims(new Claim(NovaClaims.Subject, subject));
    }
#endif

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

            // The core asks every IPermissionResolver: the test issuer's subjects also hold what CreateClientWithPermissions
            // granted. The host's resolvers answer every token, the identity provider's from its store.
            services.AddScoped<IPermissionResolver>(_ => new GrantingPermissionResolver(_grants));

            // Every module context on its own SQLite database, in place of the host's: a later AddModuleDatabase replaces
            // the earlier one, so the host's provider is never configured and the contexts have one provider each.
            services.AddModuleDatabase((_, options) => UseSqlite(options,
#if (BuiltInAuth)
                options.Options.ContextType == typeof(IdentityServerDbContext) ? _identityDatabase.ConnectionString :
#endif
#if (multitenancy)
                options.Options.ContextType == typeof(MultiTenancyDbContext) ? _tenancyDatabase.ConnectionString :
#endif
                _todosDatabase.ConnectionString));
        });
    }

    // Stores DateTimeOffset as UTC ticks, which SQLite can compare and order.
    private static DbContextOptionsBuilder UseSqlite(DbContextOptionsBuilder options, string connectionString) =>
        options.UseSqlite(connectionString).ReplaceService<IModelCustomizer, UtcTicksModelCustomizer>();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _todosDatabase.Dispose();
#if (BuiltInAuth)
            _identityDatabase.Dispose();
#endif
#if (multitenancy)
            _tenancyDatabase.Dispose();
#endif
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

    private sealed class GrantingPermissionResolver(IReadOnlyDictionary<string, string[]> grants) : IPermissionResolver
    {
        // By subject, not by issuer: the host's own provider (BuiltIn) issues its tokens as Jwt:Authority, which here is the
        // test issuer too, so only a subject CreateClientWithPermissions made is answered from the grants.
        public Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<string>>(
                user.FindFirstValue(NovaClaims.Subject) is { } subject && grants.TryGetValue(subject, out var granted)
                    ? granted
                    : []);
    }
}
