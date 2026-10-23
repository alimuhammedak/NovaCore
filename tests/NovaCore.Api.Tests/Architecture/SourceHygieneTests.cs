using System.Text.RegularExpressions;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// Line-level rules over the source under <c>src/</c>: DECISIONS Y-38 (leftover work goes to PROGRESS.md "Sonraya
/// bırakılanlar", not into TODO comments) and PLAN F2b.3 (the time comes from an injected <see cref="TimeProvider"/>).
/// </summary>
public class SourceHygieneTests
{
    private static readonly Regex UntrackedWork = new(@"\b(TODO|FIXME)\b", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    // The static clocks: DateTime(Offset).UtcNow/Now/Today, and reading TimeProvider.System directly instead of the
    // injected provider (registering TimeProvider.System as the default is fine).
    private static readonly Regex AmbientClock =
        new(@"\bDateTime(Offset)?\.(UtcNow|Now|Today)\b|\bTimeProvider\.System\.Get\w+\(", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    // DECISIONS D-021 (6): domain events are created in domain code, which has no DI. Their default timestamp is the
    // moment of creation, and the creator can set it with init. Only that line is exempt.
    private static readonly (string Path, string Line)[] AmbientClockExceptions =
    [
        (Path.Combine("src", "BuildingBlocks", "NovaCore.Abstractions", "Messaging", "IDomainEvent.cs"),
            "public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;")
    ];

    [Fact]
    public void Source_has_no_TODO_or_FIXME() =>
        Assert.Empty(LinesMatching(UntrackedWork));

    [Fact]
    public void Source_reads_the_time_from_TimeProvider() =>
        Assert.Empty(LinesMatching(AmbientClock, except: AmbientClockExceptions));

    private static IEnumerable<string> LinesMatching(Regex pattern, (string Path, string Line)[]? except = null)
    {
        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(Solution.RootDirectory, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .ToList();
        Assert.NotEmpty(sourceFiles);

        return
            from path in sourceFiles
            let relativePath = Path.GetRelativePath(Solution.RootDirectory, path)
            from line in File.ReadLines(path).Select((text, index) => (text, number: index + 1))
            where pattern.IsMatch(line.text)
                  && (except is null || !except.Contains((relativePath, line.text.Trim())))
            select $"{relativePath}:{line.number}";
    }

    private static bool IsBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) is var segments
        && (segments.Contains("bin") || segments.Contains("obj"));
}
