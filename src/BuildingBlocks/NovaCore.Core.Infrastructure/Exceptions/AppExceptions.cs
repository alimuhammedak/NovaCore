using NovaCore.Abstractions.Results;

namespace NovaCore.Core.Infrastructure.Exceptions;

/// <summary>
/// Base for application exceptions that map to a known HTTP status. Prefer <see cref="Result"/> for
/// expected failures; throw these only when an exception genuinely is the cleaner control flow.
/// </summary>
/// <param name="message">The error message.</param>
/// <param name="type">The failure category, which decides the HTTP status code.</param>
public abstract class AppException(string message, ErrorType type) : Exception(message)
{
    /// <summary>The failure category, which decides the HTTP status code.</summary>
    public ErrorType Type { get; } = type;
}

/// <summary>A requested resource does not exist (HTTP 404).</summary>
/// <param name="message">The error message.</param>
public sealed class NotFoundException(string message)
    : AppException(message, ErrorType.NotFound)
{
    /// <summary>Creates the exception with the message <c>{entity} '{key}' was not found.</c></summary>
    /// <param name="entity">The kind of resource, e.g. <c>Product</c>.</param>
    /// <param name="key">The key that was looked up.</param>
    public static NotFoundException For(string entity, object key) =>
        new($"{entity} '{key}' was not found.");
}

/// <summary>The request conflicts with the current state (HTTP 409).</summary>
/// <param name="message">The error message.</param>
public sealed class ConflictException(string message)
    : AppException(message, ErrorType.Conflict);

/// <summary>The caller is not allowed to perform the action (HTTP 403).</summary>
/// <param name="message">The error message.</param>
public sealed class ForbiddenException(string message = "You are not allowed to perform this action.")
    : AppException(message, ErrorType.Forbidden);
