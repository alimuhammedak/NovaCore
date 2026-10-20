using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NovaCore.Abstractions.Pagination;
using NovaCore.AspNetCore.Authorization;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Infrastructure;
using NovaCore.Modules.Payments.Mapping;

namespace NovaCore.Modules.Payments.Features;

/// <summary>Paged payment history. Read side — projects straight to a DTO via Mapperly.</summary>
public static class GetPayments
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", Handle)
            .WithName("GetPayments")
            .WithSummary("List payments (paged)")
            .RequirePermission(PaymentsPermissions.Read);

    private static async Task<Ok<PagedList<PaymentDto>>> Handle(
        PaymentsDbContext dbContext,
        CancellationToken cancellationToken,
        int? page = null,
        int? pageSize = null)
    {
        var pagination = new PaginationRequest(page ?? 1, pageSize ?? PaginationRequest.DefaultPageSize);

        var query = dbContext.Payments.AsNoTracking();
        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ProjectToDto()
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PagedList<PaymentDto>(items, pagination.Page, pagination.PageSize, totalCount));
    }
}
