using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NovaCore.Core.Abstractions.Pagination;
using NovaCore.Core.Web.Authorization;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Catalog.Mapping;

namespace NovaCore.Modules.Catalog.Features;

/// <summary>Paged, optionally filtered product listing. Read side of CQRS — straight to the DbContext.</summary>
public static class GetProducts
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", Handle)
            .WithName("GetProducts")
            .WithSummary("List products (paged)")
            .RequirePermission(CatalogPermissions.ProductsRead)
            .Produces<PagedList<ProductDto>>(StatusCodes.Status200OK);

    private static async Task<Ok<PagedList<ProductDto>>> Handle(
        CatalogDbContext dbContext,
        CancellationToken cancellationToken,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        bool includeInactive = false)
    {
        var pagination = new PaginationRequest(page ?? 1, pageSize ?? PaginationRequest.DefaultPageSize);

        var query = dbContext.Products.AsNoTracking();
        if (!includeInactive)
            query = query.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{search}%"));

        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ProjectToDto()
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PagedList<ProductDto>(items, pagination.Page, pagination.PageSize, totalCount));
    }
}
