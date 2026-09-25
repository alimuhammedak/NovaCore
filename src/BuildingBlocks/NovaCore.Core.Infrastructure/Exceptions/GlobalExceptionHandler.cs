using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NovaCore.Abstractions.Results;

namespace NovaCore.Core.Infrastructure.Exceptions;

/// <summary>
/// Last line of defence: turns any unhandled exception into an RFC 9457 ProblemDetails response.
/// Registered after more specific handlers (e.g. validation), so it only sees what they pass through.
/// </summary>
/// <remarks>
/// .NET 10 note: when <see cref="TryHandleAsync"/> returns true, the framework now suppresses the
/// duplicate "UnhandledException" log and the error.type metric tag. We still log here ourselves so
/// nothing is lost. To restore the old framework diagnostics, configure
/// <c>UseExceptionHandler(new ExceptionHandlerOptions { SuppressDiagnosticsCallback = _ => false })</c>.
/// </remarks>
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, type) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
            LogUnhandledException(exception, exception.Message);
        else
            LogHandledException(type, exception.Message);

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = title,
                Type = $"https://httpstatuses.io/{status}",
                // Never leak internals for 5xx; echo the message for client (4xx) errors.
                Detail = status >= StatusCodes.Status500InternalServerError ? null : exception.Message
            }
        });
    }

    private static (int Status, string Title, ErrorType Type) Map(Exception exception) => exception switch
    {
        AppException { Type: ErrorType.NotFound } => (StatusCodes.Status404NotFound, "Resource not found", ErrorType.NotFound),
        AppException { Type: ErrorType.Conflict } => (StatusCodes.Status409Conflict, "Conflict", ErrorType.Conflict),
        AppException { Type: ErrorType.Forbidden } => (StatusCodes.Status403Forbidden, "Forbidden", ErrorType.Forbidden),
        AppException { Type: ErrorType.Unauthorized } => (StatusCodes.Status401Unauthorized, "Unauthorized", ErrorType.Unauthorized),
        AppException { Type: ErrorType.Validation } => (StatusCodes.Status400BadRequest, "Validation failed", ErrorType.Validation),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found", ErrorType.NotFound),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", ErrorType.Forbidden),
        ArgumentException or FormatException => (StatusCodes.Status400BadRequest, "Bad request", ErrorType.Validation),
        OperationCanceledException => (499 /* client closed request */, "Request cancelled", ErrorType.Failure),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", ErrorType.Failure)
    };

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception: {Message}")]
    private partial void LogUnhandledException(Exception exception, string message);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Handled {ErrorType}: {Message}")]
    private partial void LogHandledException(ErrorType errorType, string message);
}
