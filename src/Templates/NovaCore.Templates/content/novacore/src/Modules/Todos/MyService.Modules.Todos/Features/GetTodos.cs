using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MyService.Modules.Todos.Authorization;
using MyService.Modules.Todos.Infrastructure;
using MyService.Modules.Todos.Mapping;
using NovaCore.Abstractions.Pagination;
using NovaCore.AspNetCore.Authorization;

namespace MyService.Modules.Todos.Features;

/// <summary>Lists todos a page at a time, projected to the DTO in SQL. A list endpoint is always paged.</summary>
public static class GetTodos
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/", Handle)
            .WithName("GetTodos")
            .WithSummary("List todos (paged)")
            .RequirePermission(TodosPermissions.ItemsRead);

    private static async Task<Ok<PagedList<TodoDto>>> Handle(
        TodosDbContext dbContext,
        CancellationToken cancellationToken,
        int? page = null,
        int? pageSize = null)
    {
        // Out-of-range values are clamped, so no caller gets more than PaginationRequest.MaxPageSize rows.
        var pagination = new PaginationRequest(page ?? 1, pageSize ?? PaginationRequest.DefaultPageSize);

        var query = dbContext.Todos.AsNoTracking();
        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.Title)
            .ThenBy(t => t.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ProjectToDto()
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PagedList<TodoDto>(items, pagination.Page, pagination.PageSize, totalCount));
    }
}
