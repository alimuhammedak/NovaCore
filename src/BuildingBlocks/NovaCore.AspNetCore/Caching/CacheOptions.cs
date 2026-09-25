namespace NovaCore.AspNetCore.Caching;

/// <summary>Bound from the <c>Cache</c> section: the default lifetime of a HybridCache entry.</summary>
internal sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
}
