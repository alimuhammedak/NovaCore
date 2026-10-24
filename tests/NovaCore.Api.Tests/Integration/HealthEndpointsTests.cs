using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Identity;
using NovaCore.Modules.Catalog.Infrastructure;
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

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Alive_endpoint_reports_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/alive", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void Every_module_database_has_a_readiness_check()
    {
        var registrations = _factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        foreach (var context in new[] { "CatalogDbContext", "IdentityServerDbContext", "PaymentsDbContext" })
        {
            var check = Assert.Single(registrations, registration => string.Equals(registration.Name, context, StringComparison.Ordinal));
            Assert.Contains("ready", check.Tags);
        }
    }

    [Fact]
    public async Task Health_endpoint_reports_unhealthy_when_a_module_database_is_unreachable()
    {
        await using var unreachable = new UnreachableCatalogApiFactory();
        var client = unreachable.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Health_endpoints_are_not_mapped_outside_development(string path)
    {
        await using var production = new ProductionApiFactory();
        // Authenticated: an anonymous caller gets 401 for any path the fallback policy guards, mapped or not (F4c.1).
        var client = production.CreateClientWithClaims(new Claim(NovaClaims.Subject, Guid.NewGuid().ToString()));

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Catalog points at a SQLite file that does not exist, so its DbContext check cannot connect. <c>Mode=ReadWrite</c>
    /// makes opening it fail rather than create it: the host's outbox dispatcher opens it too.
    /// </summary>
    private sealed class UnreachableCatalogApiFactory : NovaCoreApiFactory
    {
        private readonly string _missingFile = Path.Combine(Path.GetTempPath(), $"novacore-missing-{Guid.NewGuid():N}.db");

        protected override void UseDatabase(DbContextOptionsBuilder options)
        {
            if (options.Options.ContextType == typeof(CatalogDbContext))
                UseSqlite(options, $"Data Source={_missingFile};Mode=ReadWrite");
            else
                base.UseDatabase(options);
        }
    }
}
