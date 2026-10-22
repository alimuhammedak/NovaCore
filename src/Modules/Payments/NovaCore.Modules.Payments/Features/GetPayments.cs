using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NovaCore.Abstractions.Identity;
using NovaCore.Abstractions.Pagination;
using NovaCore.AspNetCore.Authorization;
using NovaCore.Modules.Payments.Authorization;
using NovaCore.Modules.Payments.Domain;
using NovaCore.Modules.Payments.Infrastructure;
using NovaCore.Modules.Payments.Mapping;

namespace NovaCore.Modules.Payments.Features;

/// <summary>
/// Paged payment history: the caller's own payments, or everyone's with <c>payments:read-all</c>. Read side — projects
/// straight to a DTO via Mapperly.
/// </summary>
public static class GetPayments
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", Handle)
            .WithName("GetPayments")
            .WithSummary("List your payments, or everyone's with payments:read-all (paged)")
            .RequirePermission(PaymentsPermissions.Read);

    /// <summary>The payments <paramref name="caller"/> may list: every one with <c>payments:read-all</c>, else their own.</summary>
    public static IQueryable<Payment> VisibleTo(IQueryable<Payment> payments, ICurrentUser caller) =>
        caller.HasPermission(PaymentsPermissions.ReadAll)
            ? payments
            : payments.Where(payment => payment.OwnerId == caller.UserId);

    private static async Task<Ok<PagedList<PaymentDto>>> Handle(
        PaymentsDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken,
        int? page = null,
        int? pageSize = null)
    {
        var pagination = new PaginationRequest(page ?? 1, pageSize ?? PaginationRequest.DefaultPageSize);

        var query = VisibleTo(dbContext.Payments.AsNoTracking(), currentUser);
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
