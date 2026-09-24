using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Core.Infrastructure.Persistence;
using NovaCore.Core.Web.Authorization;
using NovaCore.Core.Web.Modules;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Contracts;
using NovaCore.Modules.Catalog.Features;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Catalog.Internal;

namespace NovaCore.Modules.Catalog;

/// <summary>
/// The Catalog module. Discovered automatically by the host — registers its own DbContext, public
/// contract and validators, and maps its endpoints under <c>/api/v1/products</c>.
/// </summary>
public sealed class CatalogModule : IModule
{
    public string Name => "Catalog";

    public IServiceCollection AddModule(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<CatalogDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("Database"),
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema);
                    sql.EnableRetryOnFailure();
                });

            // Auditing (CreatedBy/ModifiedAt) is stamped centrally for every module.
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        // The module's public, in-process API for other modules.
        services.AddScoped<IProductCatalog, ProductCatalog>();

        // Declare this module's permissions so the Identity seeder can wire them to roles.
        services.AddPermissionDefinitions<CatalogPermissionProvider>();

        // FluentValidation validators (incl. the internal ones nested in feature classes).
        services.AddValidatorsFromAssembly(typeof(CatalogModule).Assembly, includeInternalTypes: true);

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
