using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Identity.Services;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// <c>/health</c> is readiness: it includes a database check for every module DbContext (tagged <c>ready</c>).
/// <c>/alive</c> is liveness. Both are mapped in Development only (docs/plan/DECISIONS.md D-022).
/// </summary>
public class HealthEndpointsTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    private readonly NovaCoreApiFactory _factory = factory;

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Alive_endpoint_reports_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void Every_module_database_has_a_readiness_check()
    {
        var registrations = _factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        foreach (var context in new[] { "CatalogDbContext", "IdentityDbContext", "PaymentsDbContext" })
        {
            var check = Assert.Single(registrations, registration => registration.Name == context);
            Assert.Contains("ready", check.Tags);
        }
    }

    [Fact]
    public async Task Health_endpoint_reports_unhealthy_when_a_module_database_is_unreachable()
    {
        await using var unreachable = new UnreachableCatalogApiFactory();
        var client = unreachable.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Health_endpoints_are_not_mapped_outside_development(string path)
    {
        // The parent owns the SQLite connections; disposing it also disposes the derived factory.
        await using var production = new ProductionApiFactory();
        var client = production
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => services.AddSingleton<IEmailSender, LoggingEmailSender>()))
            .CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Catalog points at a SQLite file that does not exist, so its DbContext check cannot connect.</summary>
    private sealed class UnreachableCatalogApiFactory : NovaCoreApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            var missingFile = Path.Combine(Path.GetTempPath(), $"novacore-missing-{Guid.NewGuid():N}.db");
            builder.ConfigureServices(services =>
                ReplaceWithSqlite<CatalogDbContext>(services, options => options.UseSqlite($"Data Source={missingFile}")));
        }
    }
}
