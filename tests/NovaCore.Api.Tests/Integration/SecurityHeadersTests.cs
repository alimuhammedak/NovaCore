using Xunit;

namespace NovaCore.Api.Tests.Integration;

public class SecurityHeadersTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    private readonly NovaCoreApiFactory _factory = factory;

    [Fact]
    public async Task Responses_carry_defensive_security_headers()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal("nosniff", Single(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", Single(response, "X-Frame-Options"));
        Assert.Equal("no-referrer", Single(response, "Referrer-Policy"));
        Assert.Contains("default-src 'none'", Single(response, "Content-Security-Policy"));
        Assert.False(response.Headers.Contains("Server"));
    }

    private static string Single(HttpResponseMessage response, string header) =>
        response.Headers.TryGetValues(header, out var values)
            ? string.Join(",", values)
            : throw new Xunit.Sdk.XunitException($"Missing header: {header}");
}
