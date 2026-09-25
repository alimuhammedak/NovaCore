using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.DependencyInjection;
using NovaCore.AspNetCore.Exceptions;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// DECISIONS Y-35 / PLAN F2b.4: only <see cref="AppException"/> messages reach the client; any other exception keeps its
/// status code but not its message. The ProblemDetails <c>instance</c> is a URI reference (R-051). The handler runs with
/// the ProblemDetails configuration <c>AddCoreInfrastructure</c> registers.
/// </summary>
public class GlobalExceptionHandlerTests
{
    private const string Secret = "Server=db;Password=hunter2";

    [Theory]
    [InlineData(nameof(ArgumentException), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(FormatException), StatusCodes.Status400BadRequest)]
    [InlineData(nameof(UnauthorizedAccessException), StatusCodes.Status403Forbidden)]
    [InlineData(nameof(KeyNotFoundException), StatusCodes.Status404NotFound)]
    [InlineData(nameof(InvalidOperationException), StatusCodes.Status500InternalServerError)]
    public async Task Other_exception_messages_are_not_sent_to_the_client(string exceptionType, int expectedStatus)
    {
        Exception exception = exceptionType switch
        {
            nameof(ArgumentException) => new ArgumentException(Secret),
            nameof(FormatException) => new FormatException(Secret),
            nameof(UnauthorizedAccessException) => new UnauthorizedAccessException(Secret),
            nameof(KeyNotFoundException) => new KeyNotFoundException(Secret),
            _ => new InvalidOperationException(Secret)
        };

        var (status, body) = await HandleAsync(exception);

        Assert.Equal(expectedStatus, status);
        Assert.DoesNotContain("hunter2", body, StringComparison.Ordinal);
        Assert.False(JsonDocument.Parse(body).RootElement.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task App_exception_messages_are_sent_to_the_client()
    {
        var (status, body) = await HandleAsync(NotFoundException.For("Product", 42));

        Assert.Equal(StatusCodes.Status404NotFound, status);
        Assert.Equal("Product '42' was not found.", JsonDocument.Parse(body).RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Instance_is_the_request_path_as_a_uri_reference_without_the_query()
    {
        var (_, body) = await HandleAsync(NotFoundException.For("Product", 42));

        Assert.Equal("/app/api/v1/products/42", JsonDocument.Parse(body).RootElement.GetProperty("instance").GetString());
    }

    private static async Task<(int Status, string Body)> HandleAsync(Exception exception)
    {
        await using var services = new ServiceCollection().AddLogging().AddCoreInfrastructure().BuildServiceProvider();
        var handler = services.GetServices<IExceptionHandler>().OfType<GlobalExceptionHandler>().Single();

        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = HttpMethods.Get;
        context.Request.PathBase = "/app";
        context.Request.Path = "/api/v1/products/42";
        context.Request.QueryString = new QueryString("?token=hunter2");
        context.Response.Body = new MemoryStream();

        Assert.True(await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken));

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return (context.Response.StatusCode, await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
    }
}
