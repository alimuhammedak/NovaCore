using Microsoft.AspNetCore.Http;
using NovaCore.Core.Abstractions.Results;

namespace NovaCore.Core.Infrastructure.Http;

/// <summary>
/// The one and only place a <see cref="Result"/> becomes an HTTP response. Endpoints stay thin:
/// they return a Result and call <c>.ToHttpResult()</c>; failures become RFC 9457 ProblemDetails.
/// </summary>
public static class ResultHttpExtensions
{
    public static IResult ToHttpResult(this Result result, int successStatusCode = StatusCodes.Status204NoContent) =>
        result.IsSuccess ? Results.StatusCode(successStatusCode) : Problem(result.Error);

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null) =>
        result.IsSuccess
            ? onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value)
            : Problem(result.Error);

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
