using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Http;

namespace NovaCore.AspNetCore.Exceptions;

/// <summary>
/// How an exception that no <see cref="IExceptionHandler"/> handled becomes an RFC 9457 response. It is the exception
/// handler middleware's own fallback (<c>StatusCodeSelector</c> and the <c>ExceptionHandler</c> delegate), not an
/// <see cref="IExceptionHandler"/>, so the handlers a project registers run first, before or after <c>AddCoreWeb</c>,
/// and the responses they write stay theirs.
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

    /// <summary>
    /// Writes the response for an exception no <see cref="IExceptionHandler"/> handled, with the status
    /// <see cref="StatusCodeFor"/> chose: a title and type per status, and only an AppException's message. The middleware
    /// calls it only on its fallback path, so a ProblemDetails a project's handler writes is never rewritten here.
    /// </summary>
    public static Task WriteResponseAsync(HttpContext context)
    {
        var feature = context.Features.GetRequiredFeature<IExceptionHandlerFeature>();
        var exception = feature.Error;
        var status = context.Response.StatusCode;

        // A RequestDelegate has no constructor to inject into; the request's services are the narrow way in (Y-25).
        return context.RequestServices.GetRequiredService<IProblemDetailsService>().TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            AdditionalMetadata = feature.Endpoint?.Metadata,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Type = $"https://httpstatuses.io/{status}",
                Title = exception switch
                {
                    AppException app => ResultHttpExtensions.TitleFor(app.Type),
                    BadHttpRequestException => ReasonPhrases.GetReasonPhrase(status),
                    _ => ResultHttpExtensions.TitleFor(ErrorType.Failure)
                },
                // Any other message (ArgumentException, framework exceptions, ...) can carry internals such as
                // connection strings or file paths (Y-35).
                Detail = exception is AppException ? exception.Message : null
            }
        }).AsTask();
    }
}
