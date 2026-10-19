using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Http;

namespace NovaCore.AspNetCore.Exceptions;

/// <summary>
/// How an exception that no <see cref="IExceptionHandler"/> handled becomes an RFC 9457 response. It is the exception
/// handler middleware's own fallback (<c>StatusCodeSelector</c> and the ProblemDetails customization), not an
/// <see cref="IExceptionHandler"/>, so the handlers a project registers run first, before or after <c>AddCoreWeb</c>.
/// <see cref="AppException"/>s keep their status and their message, which is written for clients;
/// <see cref="BadHttpRequestException"/> keeps its status; anything else is a bug: 500, its message in the log only.
/// </summary>
internal static class ExceptionMapping
{
    public static int StatusCodeFor(Exception exception) => exception switch
    {
        AppException app => ResultHttpExtensions.StatusFor(app.Type),
        BadHttpRequestException badRequest => badRequest.StatusCode,
        _ => StatusCodes.Status500InternalServerError
    };

    /// <summary>
    /// The middleware's diagnostics (the <c>UnhandledException</c> error log, the <c>error.type</c> metric tag) are for
    /// failures of the server. A client error, like a failed <see cref="Result"/>, isn't logged. Exceptions a project's
    /// handler handled stay suppressed, as the framework does by default.
    /// </summary>
    public static bool SuppressDiagnostics(ExceptionHandlerSuppressDiagnosticsContext context) =>
        context.ExceptionHandledBy == ExceptionHandledType.ExceptionHandlerService
        || context.Exception is AppException { Type: not ErrorType.Failure } or BadHttpRequestException;

    /// <summary>Gives the response for an exception the same title and type as before, and only an AppException's message.</summary>
    public static void Describe(ProblemDetailsContext context)
    {
        if (context.Exception is not { } exception)
            return;

        var status = context.ProblemDetails.Status ?? StatusCodeFor(exception);
        context.ProblemDetails.Type = $"https://httpstatuses.io/{status}";
        context.ProblemDetails.Title = exception switch
        {
            AppException app => ResultHttpExtensions.TitleFor(app.Type),
            BadHttpRequestException => ReasonPhrases.GetReasonPhrase(status),
            _ => ResultHttpExtensions.TitleFor(ErrorType.Failure)
        };

        // Any other message (ArgumentException, framework exceptions, ...) can carry internals such as connection
        // strings or file paths (Y-35).
        context.ProblemDetails.Detail = exception is AppException ? exception.Message : null;
    }
}
