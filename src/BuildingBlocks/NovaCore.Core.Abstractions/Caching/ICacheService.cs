namespace NovaCore.Core.Abstractions.Caching;

/// <summary>
/// A thin, transport-agnostic caching seam. The default implementation wraps .NET's HybridCache
/// (L1 in-memory + optional L2 distributed) so modules never take a hard dependency on it.
/// </summary>
public interface ICacheService
{
    /// <summary>Returns the cached value or invokes <paramref name="factory"/> on a miss, then caches it.</summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Produces the value on a cache miss.</param>
    /// <param name="options">Entry lifetimes; null uses the provider defaults.</param>
    /// <param name="tags">Tags to stamp the entry with, for <see cref="RemoveByTagAsync"/>.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CacheEntryOptions? options = null,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>, replacing any existing entry.</summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="options">Entry lifetimes; null uses the provider defaults.</param>
    /// <param name="tags">Tags to stamp the entry with, for <see cref="RemoveByTagAsync"/>.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    ValueTask SetAsync<T>(
        string key,
        T value,
        CacheEntryOptions? options = null,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes the entry stored under <paramref name="key"/>, if any.</summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Invalidates every entry stamped with the given tag.</summary>
    /// <param name="tag">The tag to invalidate.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}

/// <summary>Per-entry lifetime knobs, independent of the underlying cache provider.</summary>
/// <param name="Expiration">How long the entry lives overall; null uses the provider default.</param>
/// <param name="LocalCacheExpiration">How long the in-memory (L1) copy lives; null uses the provider default.</param>
public readonly record struct CacheEntryOptions(
    TimeSpan? Expiration = null,
    TimeSpan? LocalCacheExpiration = null);
