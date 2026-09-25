using System.Net;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

public class OpenApiTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    private readonly NovaCoreApiFactory _factory = factory;

    [Fact]
    public async Task OpenApi_document_is_served_and_includes_module_endpoints()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/v1/products", json);
        Assert.Contains("/api/v1/payments", json);
        Assert.Contains("/api/v1/auth/login", json);
        Assert.Contains("/api/v1/auth/register", json);
        // The bearer security scheme transformer should have registered the scheme.
        Assert.Contains("Bearer", json);
    }

    [Fact]
    public async Task Scalar_ui_is_served_in_development()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }
}
