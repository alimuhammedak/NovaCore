using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using MyService.Modules.Todos.Authorization;
using MyService.Modules.Todos.Domain;
using MyService.Modules.Todos.Infrastructure;
using NovaCore.AspNetCore.Authorization;

namespace MyService.Modules.Todos.Features;

/// <summary>
/// A vertical slice: the request with its validation, the response, the route and the handler of "create a todo", in one
/// file. The handler takes its dependencies straight from minimal-API dependency injection; there is no mediator.
/// </summary>
public static class CreateTodo
{
    public sealed record Request([Required, MaxLength(200)] string Title);

    public sealed record Response(Guid Id);

    public static void Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", Handle)
            .WithName("CreateTodo")
            .WithSummary("Create a todo")
            .RequirePermission(TodosPermissions.ItemsWrite)
            .ProducesValidationProblem();

    private static async Task<Created<Response>> Handle(
        Request request,
        TodosDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var todo = new Todo(request.Title);

        dbContext.Todos.Add(todo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/v1/todos/{todo.Id}", new Response(todo.Id));
    }
}
