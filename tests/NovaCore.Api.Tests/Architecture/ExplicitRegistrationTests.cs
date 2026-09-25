using System.Text.RegularExpressions;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// DECISIONS Y-20: no "magic" registration by assembly scanning. The host adds modules explicitly
/// (<c>AddModule&lt;TModule&gt;()</c>), and the building blocks never load or scan assemblies to find services.
/// </summary>
public class ExplicitRegistrationTests
{
    private static readonly Regex AssemblyScanning = new(
        @"\bAssembly\.Load(From|File)?\s*\(|\.GetAssemblies\s*\(|\bAssemblyName\.GetAssemblyName\s*\(|\bAssemblyLoadContext\b|\.GetTypes\s*\(|\.GetExportedTypes\s*\(");

    [Fact]
    public void Building_blocks_do_not_scan_assemblies()
    {
        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(Solution.RootDirectory, "src", "BuildingBlocks"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .ToList();
        Assert.NotEmpty(sourceFiles);

        var hits =
            from path in sourceFiles
            from line in File.ReadLines(path).Select((text, index) => (text, number: index + 1))
            where AssemblyScanning.IsMatch(line.text)
            select $"{Path.GetRelativePath(Solution.RootDirectory, path)}:{line.number}";

        Assert.Empty(hits);
    }

    private static bool IsBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) is var segments
        && (segments.Contains("bin") || segments.Contains("obj"));
}
