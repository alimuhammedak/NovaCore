using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// DECISIONS D-068: the host chooses the database provider, never a module. A module tied to a provider would tie every
/// project that uses it to that database; the host gives every module context its provider (<c>AddModuleDatabase</c>)
/// and keeps the provider's migrations and design-time factories. A provider is recognized by what it is, not by its
/// name: an assembly that defines a relational provider's options (a <see cref="RelationalOptionsExtension"/>).
/// </summary>
public partial class ModuleProviderTests
{
    [Fact]
    public void Modules_reference_no_database_provider()
    {
        var modules = Solution.ModuleAssemblies.ToList();
        Assert.NotEmpty(modules);

        var violations =
            from module in modules
            from reference in module.GetReferencedAssemblies()
            where IsDatabaseProvider(reference)
            select $"{module.GetName().Name} -> {reference.Name}";

        Assert.Empty(violations);
    }

    // The template's modules aren't compiled here, and a package a module references but doesn't use leaves no assembly
    // reference: the module projects of the repository and of the template are read too. A package is recognized by
    // its assembly, so only one this test can load: the providers the template offers are this project's references.
    [Fact]
    public void Module_projects_reference_no_database_provider_package()
    {
        var projects = Directory.EnumerateFiles(Path.Combine(Solution.RootDirectory, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Modules{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();
        Assert.Contains(projects, path => path.Contains("Templates", StringComparison.Ordinal));
        Assert.Contains(projects, path => !path.Contains("Templates", StringComparison.Ordinal));

        var violations =
            from project in projects
            from package in PackageReference().Matches(File.ReadAllText(project)).Select(match => match.Groups["id"].Value)
            where IsDatabaseProvider(new AssemblyName(package))
            select $"{Path.GetFileName(project)} -> {package}";

        Assert.Empty(violations);
    }

    private static bool IsDatabaseProvider(AssemblyName name)
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.Load(name);
        }
        catch (FileNotFoundException)
        {
            return false;
        }

        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types;
        }

        return types.Any(type => type is { IsAbstract: false } && type.IsSubclassOf(typeof(RelationalOptionsExtension)));
    }

    [GeneratedRegex("<PackageReference\\s+Include=\"(?<id>[^\"]+)\"", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex PackageReference();
}
