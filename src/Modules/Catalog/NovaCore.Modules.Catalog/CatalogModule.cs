using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Messaging;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Modules;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Contracts;
using NovaCore.Modules.Catalog.Features;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Catalog.Internal;
using NovaCore.Modules.Payments.Contracts;

namespace NovaCore.Modules.Catalog;

/// <summary>
/// The Catalog module. Added by the host with <c>AddModule&lt;CatalogModule&gt;()</c> — registers its own DbContext, public
/// contract and request validation, and maps its endpoints under <c>/api/v1/products</c>.
/// </summary>
public sealed class CatalogModule : IModule
{
    public string Name => "Catalog";

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration)
    {
        // The database is the host's choice (AddModuleDatabase); the module brings its schema and its interceptors.
        // Auditing (CreatedBy/ModifiedAt) is stamped centrally for every module.
        services.AddModuleDbContext<CatalogDbContext>(CatalogDbContext.Schema, (serviceProvider, options) =>
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

        // The module's public, in-process API for other modules.
        services.AddScoped<IProductCatalog, ProductCatalog>();

        // Reserves a payment's stock when the Payments module asks (PaymentCreated, delivered by the outbox).
        services.AddScoped<IDomainEventHandler<PaymentCreated>, ReserveStockForPayment>();

        // Declare this module's permissions so the Identity seeder can wire them to roles.
        services.AddPermissionDefinitions<CatalogPermissionProvider>();

        // Built-in validation of the request records' DataAnnotations. The source generator only covers the assembly that
        // calls AddValidation, so each module calls it itself (docs/plan/RESEARCH.md R-052).
        services.AddValidation();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/products")
            .WithTags("Catalog");

        CreateProduct.Map(group);
        GetProducts.Map(group);
        GetProductById.Map(group);
        UpdateProduct.Map(group);
        DeleteProduct.Map(group);

        return endpoints;
    }
}
