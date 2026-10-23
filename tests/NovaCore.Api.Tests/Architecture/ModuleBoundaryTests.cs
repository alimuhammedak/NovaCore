using System.Text.RegularExpressions;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>DECISIONS Y-08 (modules meet only through contracts) and Y-17 (no per-module layer projects).</summary>
public class ModuleBoundaryTests
{
    [Fact]
    public void Modules_reference_other_modules_only_through_contracts()
    {
        var modules = Solution.ModuleAssemblies.ToList();
        Assert.NotEmpty(modules);

        var violations =
            from module in modules
            let name = module.GetName().Name!
            from reference in module.GetReferencedAssemblies()
            let referenced = reference.Name!
            where referenced.StartsWith("NovaCore.Modules.", StringComparison.Ordinal)
                  && !string.Equals(referenced, name, StringComparison.Ordinal)
                  && !referenced.EndsWith(".Contracts", StringComparison.Ordinal)
            select $"{name} -> {referenced}";

        Assert.Empty(violations);
    }

    [Fact]
    public void Module_projects_are_only_the_module_and_its_contracts()
    {
        var projects = Directory
            .EnumerateFiles(Path.Combine(Solution.RootDirectory, "src", "Modules"), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();
        Assert.NotEmpty(projects);

        var allowed = new Regex(@"^NovaCore\.Modules\.[A-Za-z0-9]+(\.Contracts)?$", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

        Assert.DoesNotContain(projects, project => !allowed.IsMatch(project!));
    }
}
