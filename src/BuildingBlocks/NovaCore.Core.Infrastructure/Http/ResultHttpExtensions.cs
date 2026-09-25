using Microsoft.AspNetCore.Http;
using NovaCore.Core.Abstractions.Results;

namespace NovaCore.Core.Infrastructure.Http;

/// <summary>
/// The one and only place a <see cref="Result"/> becomes an HTTP response. Endpoints stay thin:
/// they return a Result and call <c>.ToHttpResult()</c>; failures become RFC 9457 ProblemDetails.
/// </summary>
public static class ResultHttpExtensions
{
    // Overloads instead of optional parameters: only one overload of a public method may take optional parameters, or
    // adding one later breaks callers (RS0026, docs/plan/DECISIONS.md D-015).

    /// <summary>Converts a result without a value into an HTTP response: 204 No Content on success.</summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>An empty 204 response on success, otherwise ProblemDetails.</returns>
    public static IResult ToHttpResult(this Result result) =>
        result.ToHttpResult(StatusCodes.Status204NoContent);

    /// <summary>Converts a result without a value into an HTTP response.</summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="successStatusCode">The status code for a successful result.</param>
    /// <returns>An empty response with <paramref name="successStatusCode"/> on success, otherwise ProblemDetails.</returns>
    public static IResult ToHttpResult(this Result result, int successStatusCode) =>
        result.IsSuccess ? Results.StatusCode(successStatusCode) : Problem(result.Error);

    /// <summary>Converts a result carrying a value into an HTTP response: 200 OK with the value on success.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>200 OK with the value on success, otherwise ProblemDetails.</returns>
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error);

    /// <summary>Converts a result carrying a value into an HTTP response built by <paramref name="onSuccess"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="onSuccess">Builds the success response from the value, e.g. a 201 Created.</param>
    /// <returns>The response from <paramref name="onSuccess"/> on success, otherwise ProblemDetails.</returns>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : Problem(result.Error);

    /// <summary>Translates a domain <see cref="Error"/> into a ProblemDetails response.</summary>
    public static IResult Problem(Error error)
    {
        var status = StatusFor(error.Type);
        return Results.Problem(
            detail: error.Message,
            statusCode: status,
            title: TitleFor(error.Type),
            type: $"https://httpstatuses.io/{status}",
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        _ => "An unexpected error occurred"
    };
}
