using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Auth;
using NovaCore.Modules.Identity.Services;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// Configuration is bound through the options pattern and validated on start (docs/plan/RESEARCH.md R-048): an invalid
/// value stops the host with a message naming the setting, and app configuration layered in after
/// <c>CreateBuilder</c> (as <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/> does) is honoured.
/// </summary>
public class ConfigurationValidationTests
{
    [Theory]
    [InlineData("Development", "RateLimiting:PermitLimit", "0", "PermitLimit")]
    [InlineData("Development", "RateLimiting:WindowSeconds", "0", "WindowSeconds")]
    [InlineData("Development", "Jwt:SigningKey", "shorter-than-32-bytes", "Jwt:SigningKey")]
    [InlineData("Development", "Jwt:Authority", "not-a-uri", "Jwt:Authority")]
    [InlineData("Production", "Jwt:Authority", "", "Jwt:Authority")]
    [InlineData("Production", "Jwt:Authority", "http://idp.example.com", "Jwt:Authority")]
    [InlineData("Production", "Jwt:SigningKey", "a-symmetric-key-that-is-long-enough-0123456789", "Jwt:SigningKey")]
    [InlineData("Development", "Jwt:Issuer", "", "Issuer")]
    [InlineData("Development", "Jwt:AccessTokenMinutes", "0", "AccessTokenMinutes")]
    [InlineData("Development", "Jwt:RefreshTokenDays", "0", "RefreshTokenDays")]
    [InlineData("Development", "Jwt:RefreshTokenDays", "3000000", "RefreshTokenDays")]
    [InlineData("Development", "Cors:AllowedOrigins:0", "*", "Cors:AllowedOrigins")]
    [InlineData("Development", "Cors:AllowedOrigins:0", "app.example.com", "Cors:AllowedOrigins")]
    [InlineData("Development", "Cache:DefaultExpiration", "00:00:00", "Cache:DefaultExpiration")]
    [InlineData("Development", "Database:ApplyMigrations", "yes", "Database:ApplyMigrations")]
    public async Task Invalid_configuration_stops_the_host_at_startup(
        string environment, string key, string value, string expectedInMessage)
    {
        await using var factory = new ConfiguredApiFactory(environment, new() { [key] = value });

        var error = Assert.ThrowsAny<Exception>(() => factory.Server);

        Assert.Contains(expectedInMessage, Messages(error), StringComparison.Ordinal);
    }

    [Fact]
    public async Task App_configuration_reaches_the_options()
    {
        await using var factory = new ConfiguredApiFactory(Environments.Development, new() { ["Jwt:AccessTokenMinutes"] = "7" });

        var jwt = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

        Assert.Equal(7, jwt.AccessTokenMinutes);
    }

    [Fact]
    public async Task Cache_default_expiration_applies_to_hybrid_cache_entries()
    {
        await using var factory = new ConfiguredApiFactory(Environments.Development, new() { ["Cache:DefaultExpiration"] = "00:07:00" });

        var defaults = factory.Services.GetRequiredService<IOptions<HybridCacheOptions>>().Value.DefaultEntryOptions;

        // Consumers inject HybridCache itself; AddCoreCaching only sets its default entry lifetime.
        Assert.NotNull(factory.Services.GetService<HybridCache>());
        Assert.Equal(TimeSpan.FromMinutes(7), defaults?.Expiration);
        Assert.Equal(TimeSpan.FromMinutes(7), defaults?.LocalCacheExpiration);
    }

    [Fact]
    public async Task In_development_without_an_authority_or_signing_key_a_development_key_is_used()
    {
        await using var factory = new ConfiguredApiFactory(
            Environments.Development, new() { ["Jwt:Authority"] = "", ["Jwt:SigningKey"] = "" });

        var jwt = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

        Assert.False(string.IsNullOrWhiteSpace(jwt.SigningKey));
    }

    private static string Messages(Exception exception) => exception switch
    {
        AggregateException aggregate => string.Join(" | ", aggregate.Flatten().InnerExceptions.Select(Messages)),
        { InnerException: { } inner } => $"{exception.Message} | {Messages(inner)}",
        _ => exception.Message
    };

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// The shared test host plus a few configuration overrides, layered in after the factory's own values. Outside
    /// Development it also brings an <see cref="IEmailSender"/>, so only the configuration under test can stop the host.
    /// </summary>
    private sealed class ConfiguredApiFactory(string environmentName, Dictionary<string, string?> overrides)
        : NovaCoreApiFactory
    {
        protected override string EnvironmentName => environmentName;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(overrides));
            builder.ConfigureServices(services => services.AddSingleton<IEmailSender, NoOpEmailSender>());
        }
    }
}
