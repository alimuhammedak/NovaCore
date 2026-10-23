using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MyService.Modules.Todos.Authorization;
using MyService.Modules.Todos.Infrastructure;
using MyService.Modules.Todos.Mapping;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Http;

namespace MyService.Modules.Todos.Features;

/// <summary>Reads one todo. A missing one is an expected failure: a <see cref="Result{T}"/> that becomes a 404 ProblemDetails.</summary>
public static class GetTodoById
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/{id:guid}", Handle)
            .WithName("GetTodoById")
            .WithSummary("Get a todo by id")
            .RequirePermission(TodosPermissions.ItemsRead)
            .ProducesProblem(StatusCodes.Status404NotFound);

    // The typed result is the response contract in the OpenAPI document.
    private static async Task<Results<Ok<TodoDto>, ProblemHttpResult>> Handle(
        Guid id,
        TodosDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var todo = await dbContext.Todos
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        var result = todo is null
            ? Result.Failure<TodoDto>(Error.NotFound("todo.not_found", $"Todo '{id}' was not found."))
            : Result.Success(TodoMapper.ToDto(todo));

        return result.ToHttpResult();
    }
}
