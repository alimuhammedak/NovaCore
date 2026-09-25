using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// The <c>"strict"</c> rate-limit policy on <c>/api/v1/auth</c> must be partitioned per client IP, so one caller
/// exhausting its budget can't lock everyone else out of login (R-004, D-009).
/// </summary>
public class StrictRateLimitTests(StrictRateLimitApiFactory factory) : IClassFixture<StrictRateLimitApiFactory>
{
    private const string LoginPath = "/api/v1/auth/login";

    [Fact]
    public async Task Strict_policy_limits_each_client_ip_separately()
    {
        var server = factory.Server;

        Assert.Equal(StatusCodes.Status400BadRequest, await PostEmptyLoginFromAsync(server, "203.0.113.10"));
        Assert.Equal(StatusCodes.Status429TooManyRequests, await PostEmptyLoginFromAsync(server, "203.0.113.10"));

        // A different caller still has its own budget: rejected by validation (400), not by the limiter (429).
        Assert.Equal(StatusCodes.Status400BadRequest, await PostEmptyLoginFromAsync(server, "198.51.100.20"));
    }

    // HttpClient can't choose the client IP, so the request is built directly against TestServer. That path
    // doesn't flag the request as having a body, so minimal APIs would ignore it without RequestHasBody.
    private static async Task<int> PostEmptyLoginFromAsync(TestServer server, string clientIp)
    {
        var body = Encoding.UTF8.GetBytes("""{"email":"","password":""}""");

        var context = await server.SendAsync(http =>
        {
            http.Request.Method = HttpMethods.Post;
            http.Request.Path = LoginPath;
            http.Request.ContentType = "application/json";
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
/// Host with <c>RateLimiting:PermitLimit=10</c>, so the strict policy allows 1 request per window. Passed as host
/// configuration because <c>AddCoreWeb</c> reads it eagerly; host configuration reaches <c>Program</c> as
/// <c>args</c> and overrides the factory's process-wide environment variable without racing other tests (R-026).
/// </summary>
public sealed class StrictRateLimitApiFactory : NovaCoreApiFactory
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["RateLimiting:PermitLimit"] = "10" }));

        return base.CreateHost(builder);
    }
}
