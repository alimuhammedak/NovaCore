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

    [Fact]
    public void Project_files_read_ignore_references_that_only_update_an_item()
    {
        // A reference with Update (no Include) changes an item that exists already; it adds nothing to the project.
        var directory = Directory.CreateTempSubdirectory("novacore-projectfile-");
        try
        {
            var path = Path.Combine(directory.FullName, "Sample.csproj");
            File.WriteAllText(path, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <ItemGroup>
                    <PackageReference Include="Microsoft.Extensions.Logging" />
                    <PackageReference Update="Microsoft.Extensions.Logging" PrivateAssets="all" />
                    <ProjectReference Include="..\NovaCore.Abstractions\NovaCore.Abstractions.csproj" />
                    <ProjectReference Update="..\NovaCore.Abstractions\NovaCore.Abstractions.csproj" />
                    <FrameworkReference Update="Microsoft.AspNetCore.App" />
                  </ItemGroup>
                </Project>
                """);

            var project = ProjectFile.Load(path);

            Assert.Equal(["Microsoft.Extensions.Logging"], project.Packages);
            Assert.Equal([Abstractions], project.ProjectReferences);
            Assert.Empty(project.FrameworkReferences);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
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

    private static List<ProjectFile> BuildingBlocks() => Solution
        .SourceFiles("*.csproj", "src", "BuildingBlocks")
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

            // The Directory.Build.props/targets above the project add their references to it too. An item with Update
            // and no Include changes one that exists already and adds nothing, so it is not read.
            var documents = new[] { project }.Concat(InheritedBuildFiles(path)).ToList();

            // Build-time-only packages (analyzers with PrivateAssets=all) never become package dependencies.
            var packages = Included(documents, "PackageReference")
                .Where(reference => !string.Equals(
                    (string?)reference.Attribute("PrivateAssets") ?? (string?)reference.Element("PrivateAssets"),
                    "all",
                    StringComparison.OrdinalIgnoreCase))
                .Select(reference => (string)reference.Attribute("Include")!)
                .ToList();

            // Include paths use backslashes; normalize them so this also works on Linux CI.
            var projectReferences = Included(documents, "ProjectReference")
                .Select(reference => Path.GetFileNameWithoutExtension(((string)reference.Attribute("Include")!).Replace('\\', '/')))
                .ToList();

            var frameworkReferences = Included(documents, "FrameworkReference")
                .Select(reference => (string)reference.Attribute("Include")!)
                .ToList();

            var isPackable = string.Equals(project.Descendants("IsPackable").LastOrDefault()?.Value, "true", StringComparison.OrdinalIgnoreCase);

            return new ProjectFile(Path.GetFileNameWithoutExtension(path), isPackable, packages, projectReferences, frameworkReferences);
        }

        private static IEnumerable<XElement> Included(IEnumerable<XElement> documents, string item) => documents
            .Descendants(item)
            .Where(reference => reference.Attribute("Include") is not null);

        private static IEnumerable<XElement> InheritedBuildFiles(string projectPath)
        {
            // MSBuild imports the nearest Directory.Build.props/targets only, but each of these files may import the next
            // one up; the repository has one of each at its root, so walking to the root reads what the project gets.
            for (var directory = Path.GetDirectoryName(projectPath); directory is not null; directory = Path.GetDirectoryName(directory))
            {
                foreach (var name in new[] { "Directory.Build.props", "Directory.Build.targets" })
                {
                    var file = Path.Combine(directory, name);
                    if (File.Exists(file))
                        yield return XDocument.Load(file).Root!;
                }

                if (string.Equals(directory, Solution.RootDirectory, StringComparison.OrdinalIgnoreCase))
                    yield break;
            }
        }
    }
}
