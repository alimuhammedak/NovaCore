namespace NovaCore.Core.Abstractions.Caching;

/// <summary>
/// A thin, transport-agnostic caching seam. The default implementation wraps .NET's HybridCache
/// (L1 in-memory + optional L2 distributed) so modules never take a hard dependency on it.
/// </summary>
public interface ICacheService
{
    /// <summary>Returns the cached value or invokes <paramref name="factory"/> on a miss, then caches it.</summary>
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CacheEntryOptions? options = null,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default);

    ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Invalidates every entry stamped with the given tag.</summary>
    ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}

/// <summary>Per-entry lifetime knobs, independent of the underlying cache provider.</summary>
public readonly record struct CacheEntryOptions(
    TimeSpan? Expiration = null,
    TimeSpan? LocalCacheExpiration = null);
