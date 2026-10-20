using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Modules;
using NovaCore.EntityFrameworkCore.Persistence;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// The host's module wiring, pinned: which modules are registered, in what order, the endpoints they map and the outbox
/// each module's DbContext maps. The first two were written against assembly-scanning discovery before F2a.3 switched to
/// explicit registration, which must keep both unchanged.
/// </summary>
public class ModuleRegistrationTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    private readonly NovaCoreApiFactory _factory = factory;

    [Fact]
    public void Modules_are_registered_in_order()
    {
        var modules = _factory.Services.GetServices<IModule>().Select(module => module.Name);

        Assert.Equal(["Catalog", "Payments"], modules);
    }

    [Fact]
    public void Modules_map_the_same_endpoints()
    {
        // The identity provider add-on maps its account and admin API before the modules (Program.cs).
        string[] expected =
        [
            "POST /api/v1/auth/register",
            "GET /api/v1/auth/confirm-email",
            "POST /api/v1/auth/resend-confirmation-email",
            "GET /api/v1/auth/me",
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
            "POST /api/v1/products/",
            "GET /api/v1/products/",
            "GET /api/v1/products/{id:guid}",
            "PUT /api/v1/products/{id:guid}",
            "DELETE /api/v1/products/{id:guid}",
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

    [Fact]
    public void Every_module_DbContext_maps_the_outbox()
    {
        // A module's entities write their events to its outbox (PLAN F4a.1). Without modelBuilder.AddOutbox() the first
        // event would fail its save; this catches the omission before any module raises one.
        using var scope = _factory.Services.CreateScope();
        var moduleContexts = scope.ServiceProvider.GetServices<ModuleDbContextDescriptor>()
            .Where(descriptor => descriptor.ContextType.Assembly.GetName().Name!
                .StartsWith("NovaCore.Modules.", StringComparison.Ordinal))
            .Select(descriptor => (DbContext)scope.ServiceProvider.GetRequiredService(descriptor.ContextType))
            .ToList();

        Assert.Equal(["CatalogDbContext", "PaymentsDbContext"], moduleContexts.Select(context => context.GetType().Name));
        Assert.All(moduleContexts, context =>
            Assert.Contains(context.Model.GetEntityTypes(), entityType => entityType.GetTableName() == "outbox_messages"));
    }
}
