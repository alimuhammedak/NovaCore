using System.Reflection;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>What the architecture tests inspect: the repository on disk and the first-party assemblies.</summary>
internal static class Solution
{
    private const string ModulePrefix = "NovaCore.Modules.";

    /// <summary>The directory that holds <c>NovaCore.slnx</c>, found by walking up from the test output folder.</summary>
    public static string RootDirectory { get; } = FindRootDirectory();

    /// <summary>
    /// Every first-party (<c>NovaCore.*</c>) assembly except this test project. The test project references the Api,
    /// which references every module, so all of them are copied next to the test binaries.
    /// </summary>
    public static IReadOnlyList<Assembly> ProductionAssemblies { get; } = Directory
        .EnumerateFiles(AppContext.BaseDirectory, "NovaCore.*.dll")
        .Select(path => Assembly.Load(AssemblyName.GetAssemblyName(path)))
        .Where(assembly => assembly != typeof(Solution).Assembly)
        .OrderBy(assembly => assembly.GetName().Name, StringComparer.Ordinal)
        .ToList();

    public static IEnumerable<Assembly> ModuleAssemblies =>
        ProductionAssemblies.Where(assembly => assembly.GetName().Name!.StartsWith(ModulePrefix, StringComparison.Ordinal));

    private static string FindRootDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NovaCore.slnx")))
                return directory.FullName;
        }

        throw new InvalidOperationException($"NovaCore.slnx was not found above '{AppContext.BaseDirectory}'.");
    }
}
