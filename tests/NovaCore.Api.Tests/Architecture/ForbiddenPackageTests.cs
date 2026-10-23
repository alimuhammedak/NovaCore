using System.Text.Json;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// DECISIONS Y-01 (commercial licences), Y-32 (EF InMemory in tests) and Y-33 (lazy-loading proxies). The package hook
/// blocks adding these; this catches them arriving transitively too.
/// </summary>
public class ForbiddenPackageTests
{
    private static readonly string[] BannedPackages =
    [
        "MediatR",
        "AutoMapper",
        "FluentAssertions",
        "Microsoft.EntityFrameworkCore.InMemory",
        "Microsoft.EntityFrameworkCore.Proxies",
    ];

    [Fact]
    public void No_banned_package_is_in_the_dependency_graph()
    {
        var packages = ResolvedPackages();
        Assert.Contains("Microsoft.EntityFrameworkCore", packages, StringComparer.Ordinal); // guards against reading the wrong file

        var banned = packages.Where(package => BannedPackages.Any(ban =>
            package.Equals(ban, StringComparison.OrdinalIgnoreCase)
            || package.StartsWith(ban + ".", StringComparison.OrdinalIgnoreCase)));

        Assert.Empty(banned);
    }

    // The test app's deps.json lists every package in its closure, transitive ones included, and the test project
    // references the Api, which references every module.
    private static List<string> ResolvedPackages()
    {
        var depsFile = Path.Combine(AppContext.BaseDirectory, $"{typeof(ForbiddenPackageTests).Assembly.GetName().Name}.deps.json");
        using var deps = JsonDocument.Parse(File.ReadAllText(depsFile));

        return deps.RootElement.GetProperty("libraries").EnumerateObject()
            .Where(library => string.Equals(library.Value.GetProperty("type").GetString(), "package", StringComparison.Ordinal))
            .Select(library => library.Name[..library.Name.IndexOf('/')])
            .ToList();
    }
}
