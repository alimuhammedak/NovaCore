using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MyService.Modules.Todos.Authorization;
using MyService.Modules.Todos.Infrastructure;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Http;

namespace MyService.Modules.Todos.Features;

/// <summary>
/// Completes a todo. The entity decides whether it can (<c>Todo.Complete</c>); its <see cref="Result"/> becomes a 204,
/// or a 409 ProblemDetails for a todo that is already completed.
/// </summary>
public static class CompleteTodo
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapPost("/{id:guid}/complete", Handle)
            .WithName("CompleteTodo")
            .WithSummary("Complete a todo")
            .RequirePermission(TodosPermissions.ItemsWrite)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        Guid id,
        TodosDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var todo = await dbContext.Todos.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (todo is null)
            return Result.Failure(Error.NotFound("todo.not_found", $"Todo '{id}' was not found.")).ToHttpResult();

        var result = todo.Complete();
        if (result.IsSuccess)
            await dbContext.SaveChangesAsync(cancellationToken);

        return result.ToHttpResult();
    }
}
