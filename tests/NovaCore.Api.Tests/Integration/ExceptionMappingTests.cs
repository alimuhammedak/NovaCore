using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
/// message; <c>BadHttpRequestException</c> keeps its status; a form the framework can't read is a 400 (a corrupt archive the server reads is not); every other exception is a bug: 500, its message in the
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

        var timeout = await host.CreateClient().GetAsync(new Uri($"/throw/{nameof(TimeoutException)}", UriKind.Relative));
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

        await host.CreateClient().GetAsync(new Uri("/throw/not-found", UriKind.Relative));
        Assert.DoesNotContain(log.Messages, message => message.Contains("unhandled exception", StringComparison.OrdinalIgnoreCase));

        await host.CreateClient().GetAsync(new Uri($"/throw/{nameof(InvalidOperationException)}", UriKind.Relative));
        Assert.Contains(log.Messages, message => message.Contains("unhandled exception", StringComparison.OrdinalIgnoreCase));
    }

    // Antiforgery's answer for a token it can't read or validate, a form it can't read included (S-14): the client's
    // error, not logged.
    [Fact]
    public async Task An_antiforgery_failure_is_a_bad_request_and_is_not_logged()
    {
        using var log = new LogCapture();
        await using var host = await StartAsync(builder => builder.Logging.AddProvider(log));

        var (status, problem) = await GetProblemAsync(host, $"/throw/{nameof(AntiforgeryValidationException)}");

        AssertBadRequest(status, problem, log);
    }

    // A handler's own ReadFormAsync of a body the form reader can't read (S-14): the core reports the reader's
    // InvalidDataException as the framework's BadHttpRequestException, so it is the client's error (V6e-3).
    [Theory]
    [InlineData("multipart/form-data", "--x\r\n")]                           // no boundary
    [InlineData("application/x-www-form-urlencoded", "a=1&b=2&c=3&d=4")]       // over the value count limit below
    public async Task A_form_a_handler_cannot_read_is_a_bad_request_and_is_not_logged(string contentType, string body)
    {
        using var log = new LogCapture();
        await using var host = await StartAsync(builder =>
        {
            builder.Logging.AddProvider(log);
            builder.Services.Configure<FormOptions>(options => options.ValueCountLimit = 2);
        });

        using var content = new StringContent(body, Encoding.UTF8);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        var response = await host.CreateClient().PostAsync(new Uri("/form", UriKind.Relative), content);

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        AssertBadRequest((int)response.StatusCode, await response.Content.ReadFromJsonAsync<JsonElement>(), log);
    }

    // Zip, GZip, Deflate, Brotli and Tar throw InvalidDataException too: a module's corrupt archive is a bug of the
    // server, not a malformed request, so it is a 500 and logged like any other (V6e-3).
    [Theory]
    [InlineData("zip")]
    [InlineData("gzip")]
    public async Task A_corrupt_archive_the_server_reads_is_a_server_error_and_is_logged(string kind)
    {
        using var log = new LogCapture();
        await using var host = await StartAsync(builder => builder.Logging.AddProvider(log));

        var (status, problem) = await GetProblemAsync(host, $"/throw/{kind}");

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal("An unexpected error occurred", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
        Assert.Contains(log.Messages, message => message.Contains("unhandled exception", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertBadRequest(int status, JsonElement problem, LogCapture log)
    {
        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("Bad Request", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
        Assert.DoesNotContain("hunter2", problem.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain(log.Messages, message => message.Contains("unhandled exception", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task A_malformed_JSON_body_is_a_bad_request()
    {
        // The Api host in Development, where Minimal APIs throw BadHttpRequestException for a body they can't read.
        await using var factory = new NovaCoreApiFactory();
        using var client = factory.CreateClientWithPermissions(CatalogPermissions.ProductsCreate);

        using var body = new StringContent("{ \"name\": ", Encoding.UTF8, "application/json");
        var response = await client.PostAsync(new Uri("/api/v1/products", UriKind.Relative), body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.False(problem.TryGetProperty("detail", out _));
    }

    private static Task<CoreWebTestHost> StartAsync(Action<WebApplicationBuilder>? configure = null) =>
        CoreWebTestHost.StartAsync(
            app =>
            {
                app.MapGet("/throw/{kind}", (string kind) => Throw(kind)).AllowAnonymous();
                app.MapPost("/form", async (HttpRequest request) => (await request.ReadFormAsync()).Count).AllowAnonymous();
            },
            configure: configure);

    private static string Throw(string kind)
    {
        if (kind is "zip" or "gzip")
            ReadCorruptArchive(kind);

        Exception exception = kind switch
        {
            nameof(ArgumentException) => new ArgumentException(Secret, nameof(kind)),
            nameof(FormatException) => new FormatException(Secret),
            nameof(UnauthorizedAccessException) => new UnauthorizedAccessException(Secret),
            nameof(KeyNotFoundException) => new KeyNotFoundException(Secret),
            nameof(OperationCanceledException) => new OperationCanceledException(Secret),
            nameof(TimeoutException) => new TimeoutException(Secret),
            nameof(AntiforgeryValidationException) => new AntiforgeryValidationException(Secret),
            "not-found" => NotFoundException.For("Product", 42),
            "conflict" => new ConflictException("Already there."),
            "forbidden" => new ForbiddenException(),
            _ => new InvalidOperationException(Secret)
        };
        throw exception;
    }

    // What a module reading a damaged upload or a stored archive does: both readers throw InvalidDataException.
    private static void ReadCorruptArchive(string kind)
    {
        using var corrupt = new MemoryStream(Encoding.UTF8.GetBytes(Secret));
        if (string.Equals(kind, "zip", StringComparison.Ordinal))
        {
            using var archive = new ZipArchive(corrupt, ZipArchiveMode.Read);
        }
        else
        {
            using var gzip = new GZipStream(corrupt, CompressionMode.Decompress);
            gzip.CopyTo(Stream.Null);
        }
    }

    private static async Task<(int Status, JsonElement Problem)> GetProblemAsync(CoreWebTestHost host, string path)
    {
        var response = await host.CreateClient().GetAsync(new Uri(path, UriKind.Relative));
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
