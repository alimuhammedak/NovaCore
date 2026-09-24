using NovaCore.Core.Abstractions.Results;

namespace NovaCore.Core.Infrastructure.Exceptions;

/// <summary>
/// Base for application exceptions that map to a known HTTP status. Prefer <see cref="Result"/> for
/// expected failures; throw these only when an exception genuinely is the cleaner control flow.
/// </summary>
public abstract class AppException(string message, ErrorType type) : Exception(message)
{
    public ErrorType Type { get; } = type;
}

public sealed class NotFoundException(string message)
    : AppException(message, ErrorType.NotFound)
{
    public static NotFoundException For(string entity, object key) =>
        new($"{entity} '{key}' was not found.");
}

public sealed class ConflictException(string message)
    : AppException(message, ErrorType.Conflict);

public sealed class ForbiddenException(string message = "You are not allowed to perform this action.")
    : AppException(message, ErrorType.Forbidden);
