using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace NovaCore.Api.Tests.Databases;

/// <summary>A database on a test server in a container (<paramref name="Provider"/>), or why there is none (no Docker).</summary>
internal sealed record TestDatabase(string Provider, string? ConnectionString, string? Unavailable)
{
    [MemberNotNullWhen(true, nameof(ConnectionString))]
    public bool IsAvailable => ConnectionString is not null;

    /// <summary>Skips the calling test when there is no database, naming why.</summary>
    [MemberNotNull(nameof(ConnectionString))]
    public void SkipIfUnavailable()
    {
        Assert.SkipUnless(IsAvailable, $"The {Provider} tests need Docker: {Unavailable}");
    }
}
