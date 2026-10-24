using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyService.Modules.Todos.Authorization;
using MyService.Modules.Todos.Mapping;
using Xunit;

namespace MyService.Api.Tests;

/// <summary>
/// The sample module through the real host, in tenants: a request works in the tenant it names only for a member of it,
/// whose role there (MultiTenancy:Roles in appsettings.json) grants the permissions, and it never reaches another tenant's
/// todos. Also request validation and expected failures as ProblemDetails. Each test opens the tenants it uses.
/// </summary>
public sealed class TenantTodosTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Editor = "Editor";
    private const string Viewer = "Viewer";

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task An_anonymous_caller_gets_401()
    {
        var tenant = await factory.CreateTenantAsync(Cancellation);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(TodosUri(tenant), Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_caller_who_isnt_a_member_gets_403_even_with_the_permissions()
    {
        var tenant = await factory.CreateTenantAsync(Cancellation);
        using var client = factory.CreateClientWithPermissions(TodosPermissions.ItemsRead, TodosPermissions.ItemsWrite);

        using var response = await client.GetAsync(TodosUri(tenant), Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_member_whose_role_lacks_the_permission_gets_403()
    {
        var tenant = await factory.CreateTenantAsync(Cancellation);
        using var client = await factory.CreateMemberClientAsync(Viewer, [tenant], Cancellation);

        using var response = await client.PostAsJsonAsync(TodosUri(tenant), new { title = "Write the tests" }, Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_created_todo_can_be_read_back()
    {
        var tenant = await factory.CreateTenantAsync(Cancellation);
        using var client = await factory.CreateMemberClientAsync(Editor, [tenant], Cancellation);

        using var created = await client.PostAsJsonAsync(TodosUri(tenant), new { title = "Write the tests" }, Cancellation);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var todo = await client.GetFromJsonAsync<TodoDto>(created.Headers.Location, Cancellation);
        Assert.Equal("Write the tests", todo!.Title);
        Assert.False(todo.IsCompleted);
    }

    [Fact]
    public async Task Another_tenants_todo_is_out_of_reach()
    {
        var ours = await factory.CreateTenantAsync(Cancellation);
        var theirs = await factory.CreateTenantAsync(Cancellation);
        using var client = await factory.CreateMemberClientAsync(Editor, [ours, theirs], Cancellation);
        var id = await CreateTodoAsync(client, theirs, "Theirs");

        using var read = await client.GetAsync(TodoUri(ours, id), Cancellation);
        using var completed = await client.PostAsync(TodoUri(ours, id, "/complete"), content: null, Cancellation);
        var page = await client.GetFromJsonAsync<JsonElement>(TodosUri(ours), Cancellation);

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, completed.StatusCode);
        Assert.Equal(0, page.GetProperty("totalCount").GetInt64());
        var todo = await client.GetFromJsonAsync<TodoDto>(TodoUri(theirs, id), Cancellation);
        Assert.False(todo!.IsCompleted);
    }

    [Fact]
    public async Task An_empty_title_is_a_validation_problem()
    {
        var tenant = await factory.CreateTenantAsync(Cancellation);
        using var client = await factory.CreateMemberClientAsync(Editor, [tenant], Cancellation);

        using var response = await client.PostAsJsonAsync(TodosUri(tenant), new { title = "" }, Cancellation);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        Assert.True(problem.GetProperty("errors").TryGetProperty("Title", out _));
    }

    [Fact]
    public async Task Completing_a_todo_twice_is_a_conflict()
    {
        var tenant = await factory.CreateTenantAsync(Cancellation);
        using var client = await factory.CreateMemberClientAsync(Editor, [tenant], Cancellation);
        var id = await CreateTodoAsync(client, tenant, "Ship it");

        using var first = await client.PostAsync(TodoUri(tenant, id, "/complete"), content: null, Cancellation);
        using var second = await client.PostAsync(TodoUri(tenant, id, "/complete"), content: null, Cancellation);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>(Cancellation);
        Assert.Equal("todo.already_completed", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_tenants_todos_are_listed_a_page_at_a_time()
    {
        var tenant = await factory.CreateTenantAsync(Cancellation);
        using var client = await factory.CreateMemberClientAsync(Editor, [tenant], Cancellation);
        for (var i = 0; i < 3; i++)
            await CreateTodoAsync(client, tenant, $"Listed {i}");

        var page = await client.GetFromJsonAsync<JsonElement>(
            new Uri($"{TodosUri(tenant)}?page=1&pageSize=2", UriKind.Relative), Cancellation);

        Assert.Equal(2, page.GetProperty("items").GetArrayLength());
        Assert.Equal(2, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, page.GetProperty("totalCount").GetInt64());
    }

    private static Uri TodosUri(Guid tenant) => new($"/api/v1/tenants/{tenant}/todos", UriKind.Relative);

    private static Uri TodoUri(Guid tenant, Guid id, string action = "") =>
        new($"/api/v1/tenants/{tenant}/todos/{id}{action}", UriKind.Relative);

    private static async Task<Guid> CreateTodoAsync(HttpClient client, Guid tenant, string title)
    {
        using var created = await client.PostAsJsonAsync(TodosUri(tenant), new { title }, Cancellation);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<JsonElement>(Cancellation)).GetProperty("id").GetGuid();
    }
}
