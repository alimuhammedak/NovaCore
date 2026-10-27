using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// DECISIONS Y-05 / D-003 and the package map in ANALYSIS §4. The core packages depend on Microsoft packages and the
/// BCL only; third-party packages live in optional add-ons. Dependencies point one way: Abstractions ← AspNetCore,
/// Abstractions ← EntityFrameworkCore, add-ons → the core packages. The core never depends on an add-on, a module or a
/// project outside that map.
/// </summary>
public class PackageDependencyTests
{
    private const string Abstractions = "NovaCore.Abstractions";
    private const string AspNetCore = "NovaCore.AspNetCore";
    private const string EntityFrameworkCore = "NovaCore.EntityFrameworkCore";

    private static readonly string[] CorePackages = [Abstractions, AspNetCore, EntityFrameworkCore];

    [Fact]
    public void Core_packages_reference_only_Microsoft_packages()
    {
        // Direct references only: D-003 restricts what a core package depends on, and Microsoft's own packages may
        // bring third-party ones transitively (Microsoft.Extensions.Http.Resilience brings Polly).
        var violations =
            from project in CoreProjects()
            from package in project.Packages
            where !package.StartsWith("Microsoft.", StringComparison.Ordinal)
                  && !package.StartsWith("System.", StringComparison.Ordinal)
            select $"{project.Name} -> {package}";

        Assert.Empty(violations);
    }

    [Fact]
    public void Core_code_uses_only_Microsoft_and_BCL_assemblies()
    {
        // Catches code that calls into a third-party assembly it only gets transitively.
        var violations =
            from name in CorePackages
            from reference in Assembly.Load(name).GetReferencedAssemblies()
            let referenced = reference.Name!
            where !IsMicrosoftOrBcl(referenced) && !string.Equals(referenced, Abstractions, StringComparison.Ordinal)
            select $"{name} -> {referenced}";

        Assert.Empty(violations);
    }

    [Fact]
    public void Core_packages_reference_no_first_party_project_but_Abstractions()
    {
        var projects = CoreProjects().ToDictionary(project => project.Name, StringComparer.Ordinal);

        Assert.Empty(projects[Abstractions].ProjectReferences);
        Assert.Equal([Abstractions], projects[AspNetCore].ProjectReferences);
        Assert.Equal([Abstractions], projects[EntityFrameworkCore].ProjectReferences);
    }

    [Fact]
    public void Abstractions_and_EntityFrameworkCore_do_not_depend_on_ASP_NET_Core()
    {
        var projects = CoreProjects().ToDictionary(project => project.Name, StringComparer.Ordinal);
        Assert.Empty(projects[Abstractions].FrameworkReferences);
        Assert.Empty(projects[EntityFrameworkCore].FrameworkReferences);

        Assert.All(Assembly.Load(Abstractions).GetReferencedAssemblies(), reference =>
            Assert.StartsWith("System.", reference.Name, StringComparison.Ordinal));
        Assert.DoesNotContain(Assembly.Load(EntityFrameworkCore).GetReferencedAssemblies(), reference =>
            reference.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Add_ons_reference_no_first_party_project_but_the_core_packages()
    {
        // Every packable building block outside the core is an add-on, so a new one is covered without editing this test.
        // An add-on may build on any core package (the multi-tenancy add-on deletes a tenant's rows through
        // EntityFrameworkCore, D-076 (2)), never on another add-on.
        var addOns = BuildingBlocks().Where(project => project.IsPackable && !CorePackages.Contains(project.Name)).ToList();
        Assert.NotEmpty(addOns);

        var violations =
            from addOn in addOns
            from reference in addOn.ProjectReferences
            where !CorePackages.Contains(reference)
            select $"{addOn.Name} -> {reference}";

        Assert.Empty(violations);
    }

    private static bool IsMicrosoftOrBcl(string assembly) =>
        assembly.StartsWith("System.", StringComparison.Ordinal)
        || assembly.StartsWith("Microsoft.", StringComparison.Ordinal)
        || assembly is "System" or "netstandard" or "mscorlib";

    private static List<ProjectFile> CoreProjects()
    {
        var core = BuildingBlocks().Where(project => CorePackages.Contains(project.Name)).ToList();
        Assert.Equal(CorePackages.Length, core.Count);
        Assert.All(core, project => Assert.True(project.IsPackable, $"{project.Name} should be packable."));

        return core;
    }

    private static List<ProjectFile> BuildingBlocks() => Directory
        .EnumerateFiles(Path.Combine(Solution.RootDirectory, "src", "BuildingBlocks"), "*.csproj", SearchOption.AllDirectories)
        .Select(ProjectFile.Load)
        .ToList();

    private sealed record ProjectFile(
        string Name,
        bool IsPackable,
        IReadOnlyList<string> Packages,
        IReadOnlyList<string> ProjectReferences,
        IReadOnlyList<string> FrameworkReferences)
    {
        public static ProjectFile Load(string path)
        {
            var project = XDocument.Load(path).Root!;

            // Build-time-only packages (analyzers with PrivateAssets=all) never become package dependencies.
            var packages = project.Descendants("PackageReference")
                .Where(reference => !string.Equals(
                    (string?)reference.Attribute("PrivateAssets") ?? (string?)reference.Element("PrivateAssets"),
                    "all",
                    StringComparison.OrdinalIgnoreCase))
                .Select(reference => (string)reference.Attribute("Include")!)
                .ToList();

            // Include paths use backslashes; normalize them so this also works on Linux CI.
            var projectReferences = project.Descendants("ProjectReference")
                .Select(reference => Path.GetFileNameWithoutExtension(((string)reference.Attribute("Include")!).Replace('\\', '/')))
                .ToList();

            var frameworkReferences = project.Descendants("FrameworkReference")
                .Select(reference => (string)reference.Attribute("Include")!)
                .ToList();

            var isPackable = string.Equals(project.Descendants("IsPackable").LastOrDefault()?.Value, "true", StringComparison.OrdinalIgnoreCase);

            return new ProjectFile(Path.GetFileNameWithoutExtension(path), isPackable, packages, projectReferences, frameworkReferences);
        }
    }
}
