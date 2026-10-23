using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyService.Modules.Todos.Authorization;
using MyService.Modules.Todos.Mapping;
using Xunit;

namespace MyService.Api.Tests;

/// <summary>
/// The sample module through the real host: permission checks, request validation, and expected failures that come back
/// as ProblemDetails. The tests share one database, so each one works on the todos it creates.
/// </summary>
public sealed class TodosTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly Uri TodosUri = new("/api/v1/todos", UriKind.Relative);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_anonymous_caller_gets_401()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(TodosUri, Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_caller_without_the_permission_gets_403()
    {
        using var client = factory.CreateClientWithPermissions(TodosPermissions.ItemsRead);

        using var response = await client.PostAsJsonAsync(TodosUri, new { title = "Write the tests" }, Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_created_todo_can_be_read_back()
    {
        using var client = TodosClient();

        using var created = await client.PostAsJsonAsync(TodosUri, new { title = "Write the tests" }, Cancellation);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var todo = await client.GetFromJsonAsync<TodoDto>(created.Headers.Location, Cancellation);
        Assert.Equal("Write the tests", todo!.Title);
        Assert.False(todo.IsCompleted);
    }

    [Fact]
    public async Task An_empty_title_is_a_validation_problem()
    {
        using var client = TodosClient();

        using var response = await client.PostAsJsonAsync(TodosUri, new { title = "" }, Cancellation);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        Assert.True(problem.GetProperty("errors").TryGetProperty("Title", out _));
    }

    [Fact]
    public async Task An_unknown_todo_is_not_found()
    {
        using var client = TodosClient();

        using var response = await client.GetAsync(new Uri($"/api/v1/todos/{Guid.NewGuid()}", UriKind.Relative), Cancellation);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        Assert.Equal("todo.not_found", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Completing_a_todo_twice_is_a_conflict()
    {
        using var client = TodosClient();
        var id = await CreateTodoAsync(client, "Ship it");

        using var first = await client.PostAsync(new Uri($"/api/v1/todos/{id}/complete", UriKind.Relative), content: null, Cancellation);
        using var second = await client.PostAsync(new Uri($"/api/v1/todos/{id}/complete", UriKind.Relative), content: null, Cancellation);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var todo = await client.GetFromJsonAsync<TodoDto>(new Uri($"/api/v1/todos/{id}", UriKind.Relative), Cancellation);
        Assert.True(todo!.IsCompleted);
    }

    [Fact]
    public async Task Todos_are_listed_a_page_at_a_time()
    {
        using var client = TodosClient();
        for (var i = 0; i < 3; i++)
            await CreateTodoAsync(client, $"Listed {i}");

        var page = await client.GetFromJsonAsync<JsonElement>(new Uri("/api/v1/todos?page=1&pageSize=2", UriKind.Relative), Cancellation);

        Assert.Equal(2, page.GetProperty("items").GetArrayLength());
        Assert.Equal(2, page.GetProperty("pageSize").GetInt32());
        Assert.True(page.GetProperty("totalCount").GetInt64() >= 3);
    }

    private HttpClient TodosClient() =>
        factory.CreateClientWithPermissions(TodosPermissions.ItemsRead, TodosPermissions.ItemsWrite);

    private static async Task<Guid> CreateTodoAsync(HttpClient client, string title)
    {
        using var created = await client.PostAsJsonAsync(TodosUri, new { title }, Cancellation);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }
}
