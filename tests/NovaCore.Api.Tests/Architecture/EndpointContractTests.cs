using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace NovaCore.Api.Tests.Architecture;

/// <summary>
/// DECISIONS Y-31: endpoints take and return DTOs, never a module's <c>*.Domain</c> types. Checked through ApiExplorer
/// (registered by AddOpenApi): request parameters bound from the request (not DI services) and declared response types,
/// following generic arguments, arrays and the properties of first-party DTOs. Blind spot: a handler returning a bare
/// IResult with no Produces&lt;T&gt; metadata exposes no payload type.
/// </summary>
public class EndpointContractTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    [Fact]
    public void Endpoints_do_not_accept_or_return_domain_types()
    {
        var endpoints = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .ToList();
        Assert.NotEmpty(endpoints);

        var violations = new List<string>();
        foreach (var endpoint in endpoints)
        {
            var route = $"{endpoint.HttpMethod} /{endpoint.RelativePath}";

            foreach (var parameter in endpoint.ParameterDescriptions.Where(p => p.Source != BindingSource.Services))
                violations.AddRange(DomainTypesIn(parameter.Type).Select(type => $"{route}: parameter '{parameter.Name}' exposes {type.FullName}"));

            foreach (var response in endpoint.SupportedResponseTypes)
                violations.AddRange(DomainTypesIn(response.Type).Select(type => $"{route}: {response.StatusCode} response exposes {type.FullName}"));
        }

        Assert.Empty(violations);
    }

    private static HashSet<Type> DomainTypesIn(Type? root)
    {
        var found = new HashSet<Type>();
        var visited = new HashSet<Type>();
        var pending = new Stack<Type>();
        if (root is not null)
            pending.Push(root);

        while (pending.TryPop(out var type))
        {
            if (!visited.Add(type))
                continue;

            if (IsDomainType(type))
                found.Add(type);

            if (type.HasElementType)
                pending.Push(type.GetElementType()!);

            foreach (var argument in type.GenericTypeArguments)
                pending.Push(argument);

            // A DTO that nests an entity leaks it just the same, so walk first-party DTO shapes.
            if (IsFirstParty(type) && !type.IsEnum)
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    pending.Push(property.PropertyType);
            }
        }

        return found;
    }

    private static bool IsFirstParty(Type type) => type.Namespace?.StartsWith("NovaCore.", StringComparison.Ordinal) == true;

    private static bool IsDomainType(Type type) =>
        IsFirstParty(type) && type.Namespace!.Split('.').Contains("Domain");
}
