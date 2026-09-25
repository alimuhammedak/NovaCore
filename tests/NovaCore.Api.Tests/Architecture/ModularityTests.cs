using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Modules;
using NovaCore.Modules.Catalog;
using NovaCore.Modules.Payments;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// Guardrails that keep the modular monolith honest: modules talk only through contracts, and the
/// abstractions kernel never drags in ASP.NET or EF. These fail the build the moment a boundary leaks.
/// </summary>
public class ModularityTests
{
    [Fact]
    public void Both_modules_implement_IModule()
    {
        Assert.True(typeof(IModule).IsAssignableFrom(typeof(CatalogModule)));
        Assert.True(typeof(IModule).IsAssignableFrom(typeof(PaymentsModule)));
    }

    [Fact]
    public void Payments_references_only_the_Catalog_contracts_not_the_module()
    {
        var referenced = typeof(PaymentsModule).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain("NovaCore.Modules.Catalog", referenced);
        Assert.Contains("NovaCore.Modules.Catalog.Contracts", referenced);
    }

    [Fact]
    public void Catalog_does_not_reference_the_Payments_module()
    {
        var referenced = typeof(CatalogModule).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain("NovaCore.Modules.Payments", referenced);
    }

    [Fact]
    public void Core_abstractions_stays_ui_and_persistence_agnostic()
    {
        var referenced = typeof(Result).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(referenced, n => n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, n => n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }
}
