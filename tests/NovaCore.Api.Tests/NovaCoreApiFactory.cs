using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NovaCore.Modules.Identity.Infrastructure;

namespace NovaCore.Api.Tests;

/// <summary>
/// Boots the real Program host in-memory (Development), with a fixed signing key and migrations off.
/// The Identity store is swapped to a shared in-memory SQLite database so the full auth flow
/// (register/login/refresh/permissions) can be exercised without a real SQL Server.
/// </summary>
public class NovaCoreApiFactory : WebApplicationFactory<Program>
{
    // A single open connection keeps the in-memory database alive for the factory's lifetime.
    private readonly SqliteConnection _identityConnection = new("DataSource=:memory:");

    public NovaCoreApiFactory()
    {
        // These values are read EAGERLY while Program builds services (before WebApplicationFactory's
        // in-memory config is layered in), so they must come from environment variables, which the
        // host reads at CreateBuilder time. (__ is the config section separator.)
        Environment.SetEnvironmentVariable("RateLimiting__PermitLimit", "1000000");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "novacore-integration-test-signing-key-0123456789-abcdef");

        _identityConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "novacore-integration-test-signing-key-0123456789-abcdef",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Database:ApplyMigrations"] = "false",
                ["Identity:Seed:SeedOnStartup"] = "false",
                // Keep the rate limiter wired but out of the way of the many auth calls the tests make.
                ["RateLimiting:PermitLimit"] = "100000",
                ["ConnectionStrings:Database"] =
                    "Server=localhost;Database=NovaCore_Test;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the SQL Server Identity store with shared in-memory SQLite. EF Core 10 registers
            // the provider via additive IDbContextOptionsConfiguration<TContext>, so remove EVERY
            // registration generic over IdentityDbContext (options, configuration, the context) first.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(IdentityDbContext)
                            || (d.ServiceType.IsGenericType
                                && d.ServiceType.GetGenericArguments().Contains(typeof(IdentityDbContext))))
                .ToList();
            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            // Give the SQLite context its own internal provider (the app also has SQL Server registered).
            var sqliteServices = new ServiceCollection()
                .AddEntityFrameworkSqlite()
                .BuildServiceProvider();

            services.AddDbContext<IdentityDbContext>(options => options
                .UseSqlite(_identityConnection)
                .UseInternalServiceProvider(sqliteServices));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _identityConnection.Dispose();

        base.Dispose(disposing);
    }
}
