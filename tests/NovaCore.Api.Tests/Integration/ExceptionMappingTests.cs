using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovaCore.Api.Tests.IdentityServer;
using NovaCore.AspNetCore.Exceptions;
using NovaCore.Modules.Catalog.Authorization;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// PLAN F4c.4: an unhandled exception becomes an RFC 9457 response through the exception handler middleware's own
/// fallback (<c>StatusCodeSelector</c> and the ProblemDetails customization), not through an <c>IExceptionHandler</c>, so
/// a project's handlers run first whenever they were registered. <see cref="AppException"/>s keep their status and
/// message; <c>BadHttpRequestException</c> keeps its status; every other exception is a bug: 500, its message in the
/// server log only (Y-35). The <c>instance</c> member is the request path, a URI reference without the query (R-051).
/// </summary>
public sealed class ExceptionMappingTests
{
    private const string Secret = "Server=db;Password=hunter2";

    [Theory]
    [InlineData(nameof(ArgumentException))]
    [InlineData(nameof(FormatException))]
    [InlineData(nameof(UnauthorizedAccessException))]
    [InlineData(nameof(KeyNotFoundException))]
    [InlineData(nameof(InvalidOperationException))]
    [InlineData(nameof(OperationCanceledException))]   // the server's own cancellation; a client abort never gets here
    public async Task Other_exceptions_are_server_errors_and_their_message_stays_on_the_server(string exceptionType)
    {
        await using var host = await StartAsync();

        var (status, problem) = await GetProblemAsync(host, $"/throw/{exceptionType}");

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.DoesNotContain("hunter2", problem.GetRawText(), StringComparison.Ordinal);
        Assert.False(problem.TryGetProperty("detail", out _));
        Assert.Equal("An unexpected error occurred", problem.GetProperty("title").GetString());
        Assert.Equal("https://httpstatuses.io/500", problem.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData("not-found", StatusCodes.Status404NotFound, "Resource not found", "Product '42' was not found.")]
    [InlineData("conflict", StatusCodes.Status409Conflict, "Conflict", "Already there.")]
    [InlineData("forbidden", StatusCodes.Status403Forbidden, "Forbidden", "You are not allowed to perform this action.")]
    public async Task App_exceptions_keep_their_status_and_message(string kind, int expectedStatus, string title, string detail)
    {
        await using var host = await StartAsync();

        var (status, problem) = await GetProblemAsync(host, $"/throw/{kind}");

        Assert.Equal(expectedStatus, status);
        Assert.Equal(title, problem.GetProperty("title").GetString());
        Assert.Equal(detail, problem.GetProperty("detail").GetString());
        Assert.Equal($"https://httpstatuses.io/{expectedStatus}", problem.GetProperty("type").GetString());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Instance_is_the_request_path_as_a_uri_reference_without_the_query()
    {
        await using var host = await StartAsync();

        var context = await host.Server.SendAsync(http =>
        {
            http.Request.Method = HttpMethods.Get;
            http.Request.PathBase = "/app";
            http.Request.Path = "/throw/not-found";
            http.Request.QueryString = new QueryString("?token=hunter2");
        });

        using var problem = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("/app/throw/not-found", problem.RootElement.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task A_handler_the_project_registers_after_AddCoreWeb_runs_first()
    {
        await using var host = await StartAsync(builder => builder.Services.AddExceptionHandler<TimeoutHandler>());

        var timeout = await host.CreateClient().GetAsync($"/throw/{nameof(TimeoutException)}");
        var (status, _) = await GetProblemAsync(host, "/throw/not-found");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, timeout.StatusCode);
        Assert.Equal("try again later", await timeout.Content.ReadAsStringAsync());
        // What the project's handler passes on still gets the core's mapping.
        Assert.Equal(StatusCodes.Status404NotFound, status);
    }

    [Fact]
    public async Task The_problem_details_a_project_handler_writes_are_its_own()
    {
        // The MS docs' IExceptionHandler shape: ProblemDetails written through IProblemDetailsService, exception attached.
        await using var host = await StartAsync(builder => builder.Services.AddExceptionHandler<ProblemDetailsTimeoutHandler>());

        var (status, problem) = await GetProblemAsync(host, $"/throw/{nameof(TimeoutException)}");

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Equal("Service busy", problem.GetProperty("title").GetString());
        Assert.Equal("Try again in a minute.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Server_errors_are_logged_and_client_errors_are_not()
    {
        using var log = new LogCapture();
        await using var host = await StartAsync(builder => builder.Logging.AddProvider(log));

        await host.CreateClient().GetAsync("/throw/not-found");
        Assert.DoesNotContain(log.Messages, message => message.Contains("unhandled exception", StringComparison.OrdinalIgnoreCase));

        await host.CreateClient().GetAsync($"/throw/{nameof(InvalidOperationException)}");
        Assert.Contains(log.Messages, message => message.Contains("unhandled exception", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task A_malformed_JSON_body_is_a_bad_request()
    {
        // The Api host in Development, where Minimal APIs throw BadHttpRequestException for a body they can't read.
        await using var factory = new NovaCoreApiFactory();
        using var client = factory.CreateClientWithPermissions(CatalogPermissions.ProductsCreate);

        using var body = new StringContent("{ \"name\": ", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/products", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    private static Task<CoreWebTestHost> StartAsync(Action<WebApplicationBuilder>? configure = null) =>
        CoreWebTestHost.StartAsync(
            app => app.MapGet("/throw/{kind}", (string kind) => Throw(kind)).AllowAnonymous(),
            configure: configure);

    private static string Throw(string kind)
    {
        Exception exception = kind switch
        {
            nameof(ArgumentException) => new ArgumentException(Secret),
            nameof(FormatException) => new FormatException(Secret),
            nameof(UnauthorizedAccessException) => new UnauthorizedAccessException(Secret),
            nameof(KeyNotFoundException) => new KeyNotFoundException(Secret),
            nameof(OperationCanceledException) => new OperationCanceledException(Secret),
            nameof(TimeoutException) => new TimeoutException(Secret),
            "not-found" => NotFoundException.For("Product", 42),
            "conflict" => new ConflictException("Already there."),
            "forbidden" => new ForbiddenException(),
            _ => new InvalidOperationException(Secret)
        };
        throw exception;
    }

    private static async Task<(int Status, JsonElement Problem)> GetProblemAsync(CoreWebTestHost host, string path)
    {
        var response = await host.CreateClient().GetAsync(path);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return ((int)response.StatusCode, await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    // A project's own mapping, registered after AddCoreWeb: a timeout of a dependency is a 503.
    private sealed class TimeoutHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not TimeoutException)
                return false;

            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await httpContext.Response.WriteAsync("try again later", cancellationToken);
            return true;
        }
    }

    private sealed class ProblemDetailsTimeoutHandler(IProblemDetailsService problemDetails) : IExceptionHandler
    {
        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not TimeoutException)
                return ValueTask.FromResult(false);

            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = { Status = StatusCodes.Status503ServiceUnavailable, Title = "Service busy", Detail = "Try again in a minute." }
            });
        }
    }
}
