using System.Net;
using System.Text.Json;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

public class OpenApiTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    private readonly NovaCoreApiFactory _factory = factory;

    [Fact]
    public async Task OpenApi_document_is_served_and_includes_module_endpoints()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/v1/products", json);
        Assert.Contains("/api/v1/payments", json);
        Assert.Contains("/api/v1/auth/register", json);
        Assert.Contains("/api/v1/admin/users", json);
        // The protocol endpoints and the sign-in page are not API operations.
        var paths = JsonDocument.Parse(json).RootElement.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToList();
        Assert.DoesNotContain(paths, path => path.StartsWith("/connect/", StringComparison.Ordinal));
        Assert.DoesNotContain(paths, path => path.StartsWith("/account/", StringComparison.Ordinal));
        // The bearer security scheme transformer should have registered the scheme...
        Assert.Contains("Bearer", json);
        // ...and the identity provider its sign-in flow, under the configured authority.
        Assert.Contains("\"OAuth2\"", json, StringComparison.Ordinal);
        Assert.Contains(TestIdentityProvider.Issuer + "/connect/authorize", json, StringComparison.Ordinal);
    }

    // Authorization is closed by default, so an operation that isn't anonymous answers 401 without a caller, and one that
    // names a requirement (a permission) answers 403 without it; both bodies are ProblemDetails (DAST S-15).
    [Theory]
    [InlineData("/api/v1/products", "get", true, true)]
    [InlineData("/api/v1/admin/users", "get", true, true)]
    [InlineData("/api/v1/auth/me", "get", true, false)]
    [InlineData("/api/v1/auth/logout-all", "post", true, false)]
    [InlineData("/api/v1/auth/register", "post", false, false)]
    public async Task Operations_document_the_answers_of_the_authorization_gate(
        string path, string method, bool unauthorized, bool forbidden)
    {
        var client = _factory.CreateClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative)));
        var responses = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method).GetProperty("responses");

        Assert.Equal(unauthorized, IsProblem(responses, "401"));
        Assert.Equal(forbidden, IsProblem(responses, "403"));
    }

    [Fact]
    public async Task The_authorization_gate_answers_with_problem_details()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/auth/me", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    // confirm-email answers a token it refuses with its own message, and a query value the framework can't bind with
    // ProblemDetails; the document lists both bodies under 400 (DAST S-15, ST105).
    [Fact]
    public async Task Confirm_email_documents_both_bodies_of_its_400()
    {
        var client = _factory.CreateClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative)));
        var content = document.RootElement.GetProperty("paths").GetProperty("/api/v1/auth/confirm-email").GetProperty("get")
            .GetProperty("responses").GetProperty("400").GetProperty("content");

        Assert.True(content.TryGetProperty("application/json", out _));
        Assert.True(content.TryGetProperty("application/problem+json", out _));
    }

    [Fact]
    public async Task Confirm_email_answers_a_query_it_cannot_bind_with_problem_details()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            new Uri("/api/v1/auth/confirm-email?userId=not-a-guid&token=x", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static bool IsProblem(JsonElement responses, string status) =>
        responses.TryGetProperty(status, out var response)
        && response.GetProperty("content").TryGetProperty("application/problem+json", out _);

    [Fact]
    public async Task Scalar_ui_is_served_in_development()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/scalar/v1", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
