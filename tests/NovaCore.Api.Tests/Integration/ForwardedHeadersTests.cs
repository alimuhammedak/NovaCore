using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// PLAN F4c.3: behind a reverse proxy, the client is the one the proxy names. <c>UseCoreWeb</c> applies
/// <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> first, but only from the proxies and networks listed under
/// <c>ForwardedHeaders</c>; anyone else's headers are ignored, and with an empty list every caller's are (the framework
/// default). The rate limiter partitions by the client IP the middleware restores.
/// </summary>
public sealed class ForwardedHeadersTests
{
    private const string Proxy = "10.0.0.1";
    private const string ProxyNetwork = "192.168.10.0/24";

    [Fact]
    public async Task A_trusted_proxy_names_the_client_and_its_scheme()
    {
        await using var host = await StartAsync(trusted: true);

        var direct = await EchoAsync(host, from: Proxy, forwardedFor: "203.0.113.10", forwardedProto: "https");
        var fromNetwork = await EchoAsync(host, from: "192.168.10.7", forwardedFor: "203.0.113.11", forwardedProto: "https");

        Assert.Equal("https 203.0.113.10", direct.Body);
        Assert.Equal("https 203.0.113.11", fromNetwork.Body);
    }

    [Theory]
    [InlineData("10.0.0.2")]
    [InlineData("127.0.0.1")]   // the framework trusts loopback by default; a configured list replaces that
    public async Task Headers_from_any_other_source_are_ignored(string source)
    {
        await using var host = await StartAsync(trusted: true);

        var response = await EchoAsync(host, from: source, forwardedFor: "203.0.113.10", forwardedProto: "https");

        Assert.Equal($"http {source}", response.Body);
    }

    [Fact]
    public async Task Without_trusted_proxies_the_headers_are_ignored()
    {
        await using var host = await StartAsync(trusted: false);

        var response = await EchoAsync(host, from: "127.0.0.1", forwardedFor: "203.0.113.10", forwardedProto: "https");

        Assert.Equal("http 127.0.0.1", response.Body);
    }

    [Fact]
    public async Task The_rate_limiter_partitions_by_the_forwarded_client()
    {
        await using var host = await StartAsync(trusted: true);

        // The strict policy allows one request per window here (a tenth of RateLimiting:PermitLimit = 10).
        Assert.Equal(StatusCodes.Status200OK, (await EchoAsync(host, from: Proxy, forwardedFor: "203.0.113.10")).Status);
        Assert.Equal(StatusCodes.Status429TooManyRequests, (await EchoAsync(host, from: Proxy, forwardedFor: "203.0.113.10")).Status);
        Assert.Equal(StatusCodes.Status200OK, (await EchoAsync(host, from: Proxy, forwardedFor: "198.51.100.20")).Status);

        // An untrusted source is its own client, whatever it claims.
        Assert.Equal(StatusCodes.Status200OK, (await EchoAsync(host, from: "10.0.0.2", forwardedFor: "203.0.113.30")).Status);
        Assert.Equal(StatusCodes.Status429TooManyRequests, (await EchoAsync(host, from: "10.0.0.2", forwardedFor: "203.0.113.31")).Status);
    }

    [Theory]
    [InlineData("ForwardedHeaders:KnownProxies:0", "10.0.0")]
    [InlineData("ForwardedHeaders:KnownProxies:0", "proxy.internal")]
    [InlineData("ForwardedHeaders:KnownIPNetworks:0", "10.0.0.0/33")]
    [InlineData("ForwardedHeaders:KnownIPNetworks:0", "10.0.0.1")]
    public async Task An_invalid_proxy_or_network_stops_the_host(string key, string value)
    {
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => CoreWebTestHost.StartAsync(
            _ => { },
            configure: builder => builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) { [key] = value })));

        Assert.Contains("ForwardedHeaders", exception.Message, StringComparison.Ordinal);
    }

    private static Task<CoreWebTestHost> StartAsync(bool trusted)
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal) { ["RateLimiting:PermitLimit"] = "10" };
        if (trusted)
        {
            settings["ForwardedHeaders:KnownProxies:0"] = Proxy;
            settings["ForwardedHeaders:KnownIPNetworks:0"] = ProxyNetwork;
        }

        return CoreWebTestHost.StartAsync(
            app => app.MapGet("/echo", (HttpContext context) => $"{context.Request.Scheme} {context.Connection.RemoteIpAddress}")
                .AllowAnonymous()
                .RequireRateLimiting("strict"),
            configure: builder => builder.Configuration.AddInMemoryCollection(settings));
    }

    // HttpClient can't choose the connection's address, so the request is built directly against TestServer.
    private static async Task<(int Status, string Body)> EchoAsync(
        CoreWebTestHost host, string from, string forwardedFor, string? forwardedProto = null)
    {
        var context = await host.Server.SendAsync(http =>
        {
            http.Request.Method = HttpMethods.Get;
            http.Request.Scheme = "http";
            http.Request.Path = "/echo";
            http.Request.Headers["X-Forwarded-For"] = forwardedFor;
            if (forwardedProto is not null)
                http.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
            http.Connection.RemoteIpAddress = IPAddress.Parse(from);
        });

        using var reader = new StreamReader(context.Response.Body);
        return (context.Response.StatusCode, await reader.ReadToEndAsync());
    }
}
