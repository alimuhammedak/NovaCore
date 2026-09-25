using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Modules;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// The host's module wiring, pinned: which modules are registered, in what order, and the endpoints they map. Written
/// against assembly-scanning discovery before F2a.3 switched to explicit registration, which must keep both unchanged.
/// </summary>
public class ModuleRegistrationTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    private readonly NovaCoreApiFactory _factory = factory;

    [Fact]
    public void Modules_are_registered_in_order()
    {
        var modules = _factory.Services.GetServices<IModule>().Select(module => module.Name);

        Assert.Equal(["Catalog", "Identity", "Payments"], modules);
    }

    [Fact]
    public void Modules_map_the_same_endpoints()
    {
        string[] expected =
        [
            "POST /api/v1/products/",
            "GET /api/v1/products/",
            "GET /api/v1/products/{id:guid}",
            "PUT /api/v1/products/{id:guid}",
            "DELETE /api/v1/products/{id:guid}",
            "POST /api/v1/auth/register",
            "GET /api/v1/auth/confirm-email",
            "POST /api/v1/auth/login",
            "POST /api/v1/auth/login/2fa",
            "POST /api/v1/auth/refresh",
            "GET /api/v1/auth/me",
            "POST /api/v1/auth/logout",
            "POST /api/v1/auth/logout-all",
            "POST /api/v1/auth/change-password",
            "POST /api/v1/auth/2fa/setup",
            "POST /api/v1/auth/2fa/enable",
            "POST /api/v1/auth/2fa/disable",
            "GET /api/v1/admin/users",
            "GET /api/v1/admin/roles",
            "GET /api/v1/admin/permissions",
            "POST /api/v1/admin/users/{userId:guid}/roles",
            "DELETE /api/v1/admin/users/{userId:guid}/roles/{role}",
            "POST /api/v1/payments/",
            "GET /api/v1/payments/",
        ];

        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true)
            .Select(endpoint =>
                $"{string.Join(',', endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [])} {endpoint.RoutePattern.RawText}");

        Assert.Equal(expected, endpoints);
    }
}
