using System.Text.RegularExpressions;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// DECISIONS Y-20: no "magic" registration by assembly scanning. The host adds modules explicitly
/// (<c>AddModule&lt;TModule&gt;()</c>), and nothing under <c>src/</c> (building blocks, modules, host) loads or scans
/// assemblies to find services.
/// </summary>
public class ExplicitRegistrationTests
{
    // Assembly loading and type enumeration (the GetTypes/GetExportedTypes methods and the DefinedTypes/ExportedTypes
    // properties), plus any *FromAssembly* registration helper (AddValidatorsFromAssembly, FromAssemblyOf,
    // RegisterServicesFromAssembly and the like). EF Core's ApplyConfigurationsFromAssembly configures the model, not the
    // container, so it is allowed (DECISIONS D-023).
    private static readonly Regex AssemblyScanning = new(
        @"\bAssembly\.Load(From|File)?\s*\(|\.GetAssemblies\s*\(|\bAssemblyName\.GetAssemblyName\s*\(|\bAssemblyLoadContext\b|\.GetTypes\s*\(|\.GetExportedTypes\s*\(|\.(DefinedTypes|ExportedTypes)\b|\b(?!ApplyConfigurationsFromAssembly\b)\w*FromAssembl\w*",
        RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    [Fact]
    public void Source_does_not_scan_assemblies_to_register_services()
    {
        var sourceFiles = Directory
            .EnumerateFiles(Path.Combine(Solution.RootDirectory, "src"), "*.cs", SearchOption.AllDirectories)
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
