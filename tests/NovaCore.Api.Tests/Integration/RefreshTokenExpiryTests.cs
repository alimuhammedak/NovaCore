using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NovaCore.AspNetCore.Auth;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Infrastructure;
using NovaCore.Modules.Identity.Services;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// The refresh-token lifetime is checked against the injected <see cref="TimeProvider"/> (PLAN F2b.3, R-050), so a
/// test can move the clock past it instead of waiting days.
/// </summary>
public sealed class RefreshTokenExpiryTests(FakeClockApiFactory factory)
    : IClassFixture<FakeClockApiFactory>, IAsyncLifetime
{
    private const string Email = "clock@novacore.test";
    private const string Password = "Clock!Passw0rd-2026";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async ValueTask InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        await services.GetRequiredService<IdentityDbContext>().Database.EnsureCreatedAsync();
        await services.GetRequiredService<IdentityDataSeeder>().SeedAsync();

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await users.FindByEmailAsync(Email) is null)
        {
            var user = new ApplicationUser
            {
                UserName = Email, Email = Email, EmailConfirmed = true, CreatedAtUtc = factory.Clock.GetUtcNow()
            };
            Assert.True((await users.CreateAsync(user, Password)).Succeeded);
            await users.AddToRoleAsync(user, IdentityDataSeeder.UserRole);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_refresh_token_is_rejected_once_its_lifetime_has_passed()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = (await login.Content.ReadFromJsonAsync<TokenPair>(Json))!;

        var lifetime = TimeSpan.FromDays(factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value.RefreshTokenDays);
        factory.Clock.Advance(lifetime + TimeSpan.FromSeconds(1));

        var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Contains("auth.refresh_token_expired", await refresh.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private sealed record TokenPair(string AccessToken, string RefreshToken);
}

/// <summary>
/// The shared test host with a <see cref="FakeTimeProvider"/> as the app's clock, including the memory cache behind
/// HybridCache, whose entries expire by <see cref="MemoryCacheOptions.Clock"/> rather than <see cref="TimeProvider"/>
/// (R-057). It starts at the real current time so the tokens it signs are still accepted by JWT validation, which uses
/// the real clock (R-050, R-058).
/// </summary>
public sealed class FakeClockApiFactory : NovaCoreApiFactory
{
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<TimeProvider>(Clock);
            services.Configure<MemoryCacheOptions>(options => options.Clock = new FakeSystemClock(Clock));
        });
    }

    private sealed class FakeSystemClock(TimeProvider clock) : ISystemClock
    {
        public DateTimeOffset UtcNow => clock.GetUtcNow();
    }
}
