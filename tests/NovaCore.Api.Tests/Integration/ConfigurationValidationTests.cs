using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Auth;
using NovaCore.EntityFrameworkCore.Persistence;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// Configuration is bound through the options pattern and validated on start (docs/plan/RESEARCH.md R-048): an invalid
/// value stops the host with a message naming the setting, and app configuration layered in after
/// <c>CreateBuilder</c> (as <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/> does) is honoured.
/// The settings are the core packages', so a host that must fail to start is composed of those packages and started
/// directly: a <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/> host that fails to start
/// can throw <see cref="ObjectDisposedException"/> instead of the startup error (R-055, dotnet/aspnetcore#69684).
/// </summary>
public class ConfigurationValidationTests
{
    [Theory]
    [InlineData("Development", "RateLimiting:PermitLimit", "0", "PermitLimit")]
    [InlineData("Development", "RateLimiting:WindowSeconds", "0", "WindowSeconds")]
    [InlineData("Development", "Jwt:Authority", "", "Jwt:Authority must be the absolute URI of an OpenID Connect provider")]
    [InlineData("Development", "Jwt:Authority", "not-a-uri", "Jwt:Authority")]
    [InlineData("Production", "Jwt:Authority", "", "Jwt:Authority")]
    [InlineData("Production", "Jwt:Authority", "http://idp.example.com", "Jwt:Authority")]
    [InlineData("Development", "Jwt:Audience", "", "Audience")]
    [InlineData("Development", "Cors:AllowedOrigins:0", "*", "Cors:AllowedOrigins")]
    [InlineData("Development", "Cors:AllowedOrigins:0", "app.example.com", "Cors:AllowedOrigins")]
    [InlineData("Development", "Cache:DefaultExpiration", "00:00:00", "Cache:DefaultExpiration")]
    [InlineData("Development", "Cache:PermissionExpiration", "00:00:00", "Cache:PermissionExpiration")]
    [InlineData("Development", "Database:ApplyMigrations", "yes", "Database:ApplyMigrations")]
    public async Task Invalid_configuration_stops_the_host_at_startup(
        string environment, string key, string value, string expectedInMessage)
    {
        var error = await Assert.ThrowsAnyAsync<Exception>(() => CoreWebTestHost.StartAsync(
            _ => { },
            environment,
            builder =>
            {
                builder.AddCorePersistence();
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { [key] = value });
            }));

        Assert.Contains(expectedInMessage, Messages(error), StringComparison.Ordinal);
    }

    [Fact]
    public async Task App_configuration_reaches_the_options()
    {
        await using var factory = new ConfiguredApiFactory(Environments.Development, new(StringComparer.Ordinal) { ["Jwt:Audience"] = "another.api" });

        var jwt = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

        Assert.Equal("another.api", jwt.Audience);
    }

    [Fact]
    public async Task Cache_default_expiration_applies_to_hybrid_cache_entries()
    {
        await using var factory = new ConfiguredApiFactory(Environments.Development, new(StringComparer.Ordinal) { ["Cache:DefaultExpiration"] = "00:07:00" });

        var defaults = factory.Services.GetRequiredService<IOptions<HybridCacheOptions>>().Value.DefaultEntryOptions;

        // Consumers inject HybridCache itself; AddCoreCaching only sets its default entry lifetime.
        Assert.NotNull(factory.Services.GetService<HybridCache>());
        Assert.Equal(TimeSpan.FromMinutes(7), defaults?.Expiration);
        Assert.Equal(TimeSpan.FromMinutes(7), defaults?.LocalCacheExpiration);
    }

    private static string Messages(Exception exception) => exception switch
    {
        AggregateException aggregate => string.Join(" | ", aggregate.Flatten().InnerExceptions.Select(Messages)),
        { InnerException: { } inner } => $"{exception.Message} | {Messages(inner)}",
        _ => exception.Message
    };

    /// <summary>
    /// The shared test host plus a few configuration overrides, layered in after the factory's own values. Outside
    /// Development the factory brings the keys and email sender a production host needs, so only the configuration under
    /// test can stop the host.
    /// </summary>
    private sealed class ConfiguredApiFactory(string environmentName, Dictionary<string, string?> overrides)
        : NovaCoreApiFactory
    {
        protected override string EnvironmentName => environmentName;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(overrides));
        }
    }
}
