using Microsoft.Extensions.Caching.Hybrid;
using NovaCore.Core.Abstractions.Caching;

namespace NovaCore.Core.Infrastructure.Caching;

/// <summary>
/// <see cref="ICacheService"/> on top of .NET's HybridCache: L1 in-memory + optional L2 (e.g. Redis),
/// with built-in cache-stampede protection and tag-based invalidation — all behind our own seam.
/// </summary>
public sealed class HybridCacheService(HybridCache cache) : ICacheService
{
    /// <inheritdoc />
    public ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CacheEntryOptions? options = null,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default) =>
        cache.GetOrCreateAsync(
            key,
            factory,
            ToEntryOptions(options),
            tags,
            cancellationToken);

    /// <inheritdoc />
    public ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default) =>
        cache.SetAsync(key, value, ToEntryOptions(options), tags, cancellationToken);

    /// <inheritdoc />
    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(key, cancellationToken);

    /// <inheritdoc />
    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
        cache.RemoveByTagAsync(tag, cancellationToken);

    private static HybridCacheEntryOptions? ToEntryOptions(CacheEntryOptions? options)
    {
        if (options is null)
            return null;

        return new HybridCacheEntryOptions
        {
            Expiration = options.Value.Expiration,
            LocalCacheExpiration = options.Value.LocalCacheExpiration ?? options.Value.Expiration
        };
    }
}
