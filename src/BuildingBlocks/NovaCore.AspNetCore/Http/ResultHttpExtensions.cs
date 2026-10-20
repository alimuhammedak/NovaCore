using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NovaCore.Abstractions.Results;

namespace NovaCore.AspNetCore.Http;

/// <summary>
/// The one and only place a <see cref="Result"/> becomes an HTTP response. Endpoints stay thin: they return a Result and
/// call <c>.ToHttpResult()</c>. The success response is a type in the handler's signature
/// (<c>Results&lt;Ok&lt;T&gt;, ProblemHttpResult&gt;</c>), so the OpenAPI document describes it without a separate
/// <c>Produces</c> call and the compiler rejects any other response; failures become RFC 9457 ProblemDetails, whose
/// status codes the endpoint declares with <c>ProducesProblem</c>.
/// </summary>
public static class ResultHttpExtensions
{
    // Overloads instead of optional parameters: only one overload of a public method may take optional parameters, or
    // adding one later breaks callers (RS0026, docs/plan/DECISIONS.md D-015).

    /// <summary>Converts a result without a value into an HTTP response: 204 No Content on success.</summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>An empty 204 response on success, otherwise ProblemDetails.</returns>
    public static Results<NoContent, ProblemHttpResult> ToHttpResult(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : Problem(result.Error);

    /// <summary>Converts a result carrying a value into an HTTP response: 200 OK with the value on success.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>200 OK with the value on success, otherwise ProblemDetails.</returns>
    public static Results<Ok<T>, ProblemHttpResult> ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : Problem(result.Error);

    /// <summary>
    /// Converts a result carrying a value into the typed response <paramref name="onSuccess"/> builds from it, e.g.
    /// <c>value =&gt; TypedResults.Created(location, value)</c>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TSuccess">The success response type, which describes the response in the OpenAPI document.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="onSuccess">Builds the success response from the value.</param>
    /// <returns>The response from <paramref name="onSuccess"/> on success, otherwise ProblemDetails.</returns>
    public static Results<TSuccess, ProblemHttpResult> ToHttpResult<T, TSuccess>(
        this Result<T> result, Func<T, TSuccess> onSuccess)
        where TSuccess : IResult
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        return result.IsSuccess ? onSuccess(result.Value) : Problem(result.Error);
    }

    /// <summary>Translates a domain <see cref="Error"/> into a ProblemDetails response.</summary>
    /// <param name="error">The failure, whose type decides the status code and whose code and message reach the client.</param>
    /// <returns>A ProblemDetails response with the error's status, its message as <c>detail</c> and its code as <c>code</c>.</returns>
    public static ProblemHttpResult Problem(Error error)
    {
        var status = StatusFor(error.Type);
        return TypedResults.Problem(
            detail: error.Message,
            statusCode: status,
            title: TitleFor(error.Type),
            type: $"https://httpstatuses.io/{status}",
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    // Shared with the exception mapping, so an AppException and a failed Result of the same type answer alike.
    internal static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    internal static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        _ => "An unexpected error occurred"
    };
}
