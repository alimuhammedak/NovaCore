using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Time.Testing;

namespace NovaCore.Api.Tests;

/// <summary>The test host in Production, with what a production host must bring (see <see cref="NovaCoreApiFactory"/>).</summary>
public sealed class ProductionApiFactory : NovaCoreApiFactory
{
    protected override string EnvironmentName => Environments.Production;
}

/// <summary>
/// The test host with a fake clock: <see cref="TimeProvider"/> and the memory cache behind HybridCache, whose entries
/// expire by <see cref="MemoryCacheOptions.Clock"/> rather than <see cref="TimeProvider"/> (R-057). It starts at the real
/// current time so tokens stamped from it are still accepted by JWT validation, which uses the real clock (R-050, R-058).
/// </summary>
public sealed class FakeClockApiFactory : NovaCoreApiFactory
{
    public FakeTimeProvider Clock { get; } = new(TimeProvider.System.GetUtcNow());

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
