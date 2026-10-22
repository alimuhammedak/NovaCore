using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Authorization;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// Every validated request type, pinned: an invalid body is rejected before the handler runs, with a 400 validation
/// ProblemDetails that names the failing fields. Written against the FluentValidation filter before F2c.1 moved the rules
/// to DataAnnotations and .NET 10's built-in validation, which must keep the status, type, instance, traceId and error
/// keys (docs/plan/DECISIONS.md D-023). Titles and messages are for humans and may change.
/// </summary>
public class ValidationTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    private const string SomeId = "0199a2f5-6c1e-7d10-9a4b-5c6d7e8f9a0b";

    private readonly NovaCoreApiFactory _factory = factory;

    public static TheoryData<string, string, string, string[]> InvalidRequests => new()
    {
        { "POST", "/api/v1/products", Json(new { name = "", description = new string('x', 2001), price = 0, stock = -1 }), ["Description", "Name", "Price", "Stock"] },
        { "PUT", $"/api/v1/products/{SomeId}", Json(new { name = " ", description = new string('x', 2001), price = -1, stock = -1 }), ["Description", "Name", "Price", "Stock"] },
        { "POST", "/api/v1/payments", Json(new { productId = Guid.Empty, quantity = 1 }), ["ProductId"] },
        { "POST", "/api/v1/payments", Json(new { productId = SomeId, quantity = 0 }), ["Quantity"] },
        { "POST", "/api/v1/auth/register", Json(new { email = "not-an-email", password = "too-short", displayName = new string('x', 129) }), ["DisplayName", "Email", "Password"] },
        { "POST", "/api/v1/auth/resend-confirmation-email", Json(new { email = "not-an-email" }), ["Email"] },
        { "POST", "/api/v1/auth/change-password", Json(new { currentPassword = "", newPassword = "too-short" }), ["CurrentPassword", "NewPassword"] },
        { "POST", "/api/v1/auth/2fa/setup", Json(new { password = "" }), ["Password"] },
        { "POST", "/api/v1/auth/2fa/enable", Json(new { password = "", code = "12345" }), ["Code", "Password"] },
        { "POST", "/api/v1/auth/2fa/disable", Json(new { code = "123456789" }), ["Code", "Password"] },
        { "POST", $"/api/v1/admin/users/{SomeId}/roles", Json(new { role = "" }), ["Role"] },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Invalid_request_is_rejected_with_a_validation_problem(
        string method, string path, string body, string[] expectedFields)
    {
        // Every declared permission, as an administrator holds them, so the request reaches validation on every endpoint.
        using var client = _factory.CreateClientWithPermissions(_factory.Services
            .GetServices<IPermissionDefinitionProvider>()
            .SelectMany(provider => provider.GetPermissions())
            .Select(permission => permission.Name)
            .ToArray());
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = problem.RootElement;
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", root.GetProperty("type").GetString());
        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.Equal(path, root.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));

        var errors = root.GetProperty("errors").EnumerateObject().ToList();
        Assert.Equal(expectedFields, errors.Select(error => error.Name).Order(StringComparer.Ordinal));
        Assert.All(errors, error => Assert.NotEqual(0, error.Value.GetArrayLength()));
    }

    private static string Json(object body) => JsonSerializer.Serialize(body, JsonSerializerOptions.Web);
}
