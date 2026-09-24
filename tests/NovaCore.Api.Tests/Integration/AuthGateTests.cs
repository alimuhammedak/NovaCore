using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>Authentication/validation gates that reject before any handler touches the database.</summary>
public class AuthGateTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    private readonly NovaCoreApiFactory _factory = factory;

    [Fact]
    public async Task Permission_protected_endpoint_returns_401_without_a_token()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/products",
            new { name = "Widget", price = 9.99m, stock = 5 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reading_products_requires_authentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_short_password_returns_400_validation()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "someone@example.com", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("errors", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_with_missing_fields_returns_400_validation()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
