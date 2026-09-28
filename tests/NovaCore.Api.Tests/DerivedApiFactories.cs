using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Time.Testing;
using NovaCore.Modules.Catalog.Domain;
using NovaCore.Modules.Catalog.Infrastructure;

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
public class FakeClockApiFactory : NovaCoreApiFactory
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

/// <summary>
/// The fake-clock host for concurrency tests: <see cref="ChangeStockBeforeNextSave"/> makes another writer change a
/// product's stock between the next save's read of that product and its write.
/// </summary>
public sealed class StockRaceApiFactory : FakeClockApiFactory
{
    private readonly CompetingStockWrite _competingWrite;

    public StockRaceApiFactory() => _competingWrite = new CompetingStockWrite(CatalogConnectionString);

    /// <summary>
    /// Once: the next Catalog save that modifies or deletes <paramref name="productId"/> first sees another writer set its
    /// stock to <paramref name="stock"/>, as a concurrent sale would.
    /// </summary>
    public void ChangeStockBeforeNextSave(Guid productId, int stock) => _competingWrite.Arm(productId, stock);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services => ReplaceWithSqlite<CatalogDbContext>(services,
            options => options.UseSqlite(CatalogConnectionString).AddInterceptors(_competingWrite)));
    }

    // Runs before the save sends its commands, so the other writer's change lands after the read and before the write.
    private sealed class CompetingStockWrite(string connectionString) : SaveChangesInterceptor
    {
        private readonly Lock _gate = new();
        private (Guid ProductId, int Stock)? _armed;

        public void Arm(Guid productId, int stock)
        {
            lock (_gate)
                _armed = (productId, stock);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Take(eventData.Context!) is { } write)
            {
                await using var other = new CatalogDbContext(
                    new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connectionString).Options);
                await other.Products
                    .Where(product => product.Id == write.ProductId)
                    .ExecuteUpdateAsync(set => set.SetProperty(product => product.Stock, write.Stock), cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private (Guid ProductId, int Stock)? Take(DbContext context)
        {
            lock (_gate)
            {
                if (_armed is not { } write
                    || !context.ChangeTracker.Entries<Product>().Any(entry =>
                        entry.Entity.Id == write.ProductId && entry.State is EntityState.Modified or EntityState.Deleted))
                    return null;

                _armed = null;
                return write;
            }
        }
    }
}
