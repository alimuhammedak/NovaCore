using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NovaCore.Abstractions.Identity;
using NovaCore.Abstractions.Pagination;
using NovaCore.AspNetCore.Authorization;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Infrastructure;
using NovaCore.Modules.Catalog.Mapping;

namespace NovaCore.Modules.Catalog.Features;

/// <summary>
/// Paged, optionally filtered product listing. Read side of CQRS — straight to the DbContext. <c>includeInactive</c> adds
/// the inactive products only for a caller with <c>catalog:products:update</c> (D-102 (8)); anyone else gets the active
/// ones.
/// </summary>
public static class GetProducts
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", Handle)
            .WithName("GetProducts")
            .WithSummary("List products (paged)")
            .RequirePermission(CatalogPermissions.ProductsRead);

    private static async Task<Ok<PagedList<ProductDto>>> Handle(
        CatalogDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken,
        int? page = null,
        int? pageSize = null,
        string? search = null,
        bool includeInactive = false)
    {
        var pagination = new PaginationRequest(page ?? 1, pageSize ?? PaginationRequest.DefaultPageSize);

        var query = dbContext.Products.AsNoTracking();
        if (!includeInactive || !currentUser.HasPermission(CatalogPermissions.ProductsUpdate))
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
