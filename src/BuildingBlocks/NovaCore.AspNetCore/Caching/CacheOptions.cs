namespace NovaCore.AspNetCore.Caching;

/// <summary>Bound from the <c>Cache</c> section: how long HybridCache entries live.</summary>
internal sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>The lifetime of an entry cached without its own options.</summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long a caller's resolved permissions are cached, so the longest a role or permission change takes to reach a
    /// signed-in caller. Its own setting, so raising <see cref="DefaultExpiration"/> never stretches it (D-024).
    /// </summary>
    public TimeSpan PermissionExpiration { get; set; } = TimeSpan.FromMinutes(5);
}
