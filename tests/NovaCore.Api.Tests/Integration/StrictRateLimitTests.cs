using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// The <c>"strict"</c> rate-limit policy on the sign-in endpoints must be partitioned per client IP, so one caller
/// exhausting its budget can't lock everyone else out (R-004, D-009). Exercised on the token endpoint, which the identity
/// provider guards with it, as are the sign-in page and the account API.
/// </summary>
public class StrictRateLimitTests(StrictRateLimitApiFactory factory) : IClassFixture<StrictRateLimitApiFactory>
{
    private const string TokenPath = "/connect/token";

    [Fact]
    public async Task Strict_policy_limits_each_client_ip_separately()
    {
        var server = factory.Server;

        Assert.Equal(StatusCodes.Status400BadRequest, await PostEmptyTokenRequestFromAsync(server, "203.0.113.10"));
        Assert.Equal(StatusCodes.Status429TooManyRequests, await PostEmptyTokenRequestFromAsync(server, "203.0.113.10"));

        // A different caller still has its own budget: rejected by the provider (400), not by the limiter (429).
        Assert.Equal(StatusCodes.Status400BadRequest, await PostEmptyTokenRequestFromAsync(server, "198.51.100.20"));
    }

    // HttpClient can't choose the client IP, so the request is built directly against TestServer. That path doesn't
    // flag the request as having a body, so it is set explicitly (RequestHasBody). HTTPS: OpenIddict refuses plain HTTP.
    private static async Task<int> PostEmptyTokenRequestFromAsync(TestServer server, string clientIp)
    {
        var body = Encoding.UTF8.GetBytes("grant_type=");

        var context = await server.SendAsync(http =>
        {
            http.Request.Method = HttpMethods.Post;
            http.Request.Scheme = "https";
            http.Request.Path = TokenPath;
            http.Request.ContentType = "application/x-www-form-urlencoded";
            http.Request.ContentLength = body.Length;
            http.Request.Body = new MemoryStream(body);
            http.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestHasBody());
            http.Connection.RemoteIpAddress = IPAddress.Parse(clientIp);
        });

        return context.Response.StatusCode;
    }

    private sealed class RequestHasBody : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }
}

/// <summary>
/// Host with <c>RateLimiting:PermitLimit=10</c>, so the strict policy allows 1 request per window. Added after the base
/// factory's configuration, so it wins over the base factory's high test limit.
/// </summary>
public sealed class StrictRateLimitApiFactory : NovaCoreApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["RateLimiting:PermitLimit"] = "10" }));
    }
}
