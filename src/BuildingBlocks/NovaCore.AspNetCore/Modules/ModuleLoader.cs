using System.Reflection;

namespace NovaCore.AspNetCore.Modules;

/// <summary>
/// Discovers <see cref="IModule"/> implementations from the assemblies shipped alongside the host.
/// Scans the deploy directory (not just statically referenced assemblies) so a module is found even
/// when the host references it purely for composition and uses none of its types directly.
/// </summary>
public static class ModuleLoader
{
    /// <summary>
    /// Finds and instantiates every module whose assembly name starts with one of <paramref name="assemblyPrefixes"/>.
    /// </summary>
    public static IReadOnlyList<IModule> Discover(params string[] assemblyPrefixes)
    {
        if (assemblyPrefixes.Length == 0)
            assemblyPrefixes = ["NovaCore.Modules."];

        bool Matches(string? name) =>
            name is not null && assemblyPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        // Already-loaded module assemblies...
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => Matches(a.GetName().Name))
            .ToDictionary(a => a.GetName().Name!, StringComparer.OrdinalIgnoreCase);

        // ...plus any module assemblies on disk that haven't been loaded yet.
        var baseDir = AppContext.BaseDirectory;
        foreach (var dll in Directory.EnumerateFiles(baseDir, "*.dll"))
        {
            var name = Path.GetFileNameWithoutExtension(dll);
            if (!Matches(name) || assemblies.ContainsKey(name))
                continue;

            try
            {
                assemblies[name] = Assembly.Load(AssemblyName.GetAssemblyName(dll));
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException)
            {
                // Not a managed/loadable assembly — skip it.
            }
        }

        return assemblies.Values
            .SelectMany(GetLoadableTypes)
            .Where(t => typeof(IModule).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .Select(t => (IModule)Activator.CreateInstance(t)!)
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
