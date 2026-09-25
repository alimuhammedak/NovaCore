using System.Runtime.CompilerServices;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>DECISIONS Y-15 (no repository/unit-of-work wrappers over EF) and Y-29 (no helper/util/manager buckets).</summary>
public class ForbiddenTypeNameTests
{
    private static readonly string[] BucketSuffixes = ["Helper", "Helpers", "Util", "Utils", "Utility", "Manager", "Common"];

    [Fact]
    public void No_repository_or_unit_of_work_abstractions()
    {
        var offenders = FirstPartyTypes()
            .Where(type => NameOf(type) is "IRepository" or "IUnitOfWork"
                           || (type.IsGenericTypeDefinition && NameOf(type).EndsWith("Repository", StringComparison.Ordinal)))
            .Select(type => type.FullName);

        Assert.Empty(offenders);
    }

    [Fact]
    public void No_helper_util_or_manager_types()
    {
        var offenders = FirstPartyTypes()
            .Where(type => BucketSuffixes.Any(suffix => NameOf(type).EndsWith(suffix, StringComparison.Ordinal)))
            .Select(type => type.FullName);

        Assert.Empty(offenders);
    }

    // Only NovaCore's own namespaces: source generators (e.g. Microsoft.AspNetCore.OpenApi's XML-comment support) emit
    // file-local types of their own into our assemblies.
    private static List<Type> FirstPartyTypes()
    {
        var types = Solution.ProductionAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Namespace?.StartsWith("NovaCore.", StringComparison.Ordinal) == true
                           && !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .ToList();
        Assert.NotEmpty(types);

        return types;
    }

    // Generic type names carry an arity suffix ("Repository`1").
    private static string NameOf(Type type) => type.Name.Split('`')[0];
}
