using System.Text.RegularExpressions;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// Line-level rules over the source under <c>src/</c>: DECISIONS Y-38 (leftover work goes to PROGRESS.md "Sonraya
/// bırakılanlar", not into TODO comments) and PLAN F2b.3 (the time comes from an injected <see cref="TimeProvider"/>).
/// </summary>
public class SourceHygieneTests
{
    private static readonly Regex UntrackedWork = new(@"\b(TODO|FIXME)\b");
    private static readonly Regex AmbientClock = new(@"\bDateTime(Offset)?\.(UtcNow|Now|Today)\b");

    // DECISIONS D-021 (6): domain events are created in domain code, which has no DI. Their default timestamp is the
    // moment of creation, and the creator can set it with init.
    private static readonly string[] AmbientClockAllowedIn =
        [Path.Combine("src", "BuildingBlocks", "NovaCore.Abstractions", "Messaging", "IDomainEvent.cs")];

    [Fact]
    public void Source_has_no_TODO_or_FIXME() =>
        Assert.Empty(LinesMatching(UntrackedWork));

    [Fact]
    public void Source_reads_the_time_from_TimeProvider() =>
        Assert.Empty(LinesMatching(AmbientClock, except: AmbientClockAllowedIn));

    private static IEnumerable<string> LinesMatching(Regex pattern, string[]? except = null)
    {
        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(Solution.RootDirectory, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .ToList();
        Assert.NotEmpty(sourceFiles);

        return
            from path in sourceFiles
            let relativePath = Path.GetRelativePath(Solution.RootDirectory, path)
            where except is null || !except.Contains(relativePath)
            from line in File.ReadLines(path).Select((text, index) => (text, number: index + 1))
            where pattern.IsMatch(line.text)
            select $"{relativePath}:{line.number}";
    }

    private static bool IsBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) is var segments
        && (segments.Contains("bin") || segments.Contains("obj"));
}
