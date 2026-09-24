namespace NovaCore.Core.Abstractions.Results;

/// <summary>
/// The category of a failure. Maps cleanly onto an HTTP status code at the edge,
/// but stays transport-agnostic here in the abstractions layer.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}

/// <summary>
/// A machine-readable, immutable description of why an operation failed.
/// Use these for <em>expected</em> failures; reserve exceptions for the truly exceptional.
/// </summary>
public readonly record struct Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>The absence of an error.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public override string ToString() => $"{Type}:{Code}";
}
