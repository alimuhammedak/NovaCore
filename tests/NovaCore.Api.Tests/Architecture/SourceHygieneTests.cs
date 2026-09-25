using System.Text.RegularExpressions;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>DECISIONS Y-38: leftover work goes to PROGRESS.md ("Sonraya bırakılanlar"), not into TODO comments.</summary>
public class SourceHygieneTests
{
    private static readonly Regex UntrackedWork = new(@"\b(TODO|FIXME)\b");

    [Fact]
    public void Source_has_no_TODO_or_FIXME()
    {
        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(Solution.RootDirectory, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .ToList();
        Assert.NotEmpty(sourceFiles);

        var hits =
            from path in sourceFiles
            from line in File.ReadLines(path).Select((text, index) => (text, number: index + 1))
            where UntrackedWork.IsMatch(line.text)
            select $"{Path.GetRelativePath(Solution.RootDirectory, path)}:{line.number}";

        Assert.Empty(hits);
    }

    private static bool IsBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) is var segments
        && (segments.Contains("bin") || segments.Contains("obj"));
}
