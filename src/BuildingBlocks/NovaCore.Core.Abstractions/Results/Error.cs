using System.Diagnostics.CodeAnalysis;

namespace NovaCore.Core.Abstractions.Results;

/// <summary>
/// The category of a failure. Maps cleanly onto an HTTP status code at the edge,
/// but stays transport-agnostic here in the abstractions layer.
/// </summary>
public enum ErrorType
{
    /// <summary>A general failure that fits no more specific category (HTTP 500 at the edge).</summary>
    Failure = 0,

    /// <summary>The input was invalid (HTTP 400).</summary>
    Validation = 1,

    /// <summary>The requested resource does not exist (HTTP 404).</summary>
    NotFound = 2,

    /// <summary>The request conflicts with the current state, e.g. a duplicate (HTTP 409).</summary>
    Conflict = 3,

    /// <summary>The caller is not authenticated (HTTP 401).</summary>
    Unauthorized = 4,

    /// <summary>The caller is authenticated but not allowed (HTTP 403).</summary>
    Forbidden = 5
}

/// <summary>
/// A machine-readable, immutable description of why an operation failed.
/// Use these for <em>expected</em> failures; reserve exceptions for the truly exceptional.
/// </summary>
/// <param name="Code">A stable, machine-readable identifier such as <c>Product.NotFound</c>.</param>
/// <param name="Message">A human-readable explanation.</param>
/// <param name="Type">The failure category, which decides the HTTP status code at the edge.</param>
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
    Justification = "Only collides with the Visual Basic 'Error' keyword; NovaCore targets C# and VB callers can still write [Error] (docs/plan/DECISIONS.md D-011, R-034).")]
public readonly record struct Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>The absence of an error.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>Creates an <see cref="ErrorType.Validation"/> error.</summary>
    /// <param name="code">A stable, machine-readable identifier.</param>
    /// <param name="message">A human-readable explanation.</param>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Creates an <see cref="ErrorType.NotFound"/> error.</summary>
    /// <param name="code">A stable, machine-readable identifier.</param>
    /// <param name="message">A human-readable explanation.</param>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Creates an <see cref="ErrorType.Conflict"/> error.</summary>
    /// <param name="code">A stable, machine-readable identifier.</param>
    /// <param name="message">A human-readable explanation.</param>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>Creates an <see cref="ErrorType.Unauthorized"/> error.</summary>
    /// <param name="code">A stable, machine-readable identifier.</param>
    /// <param name="message">A human-readable explanation.</param>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>Creates an <see cref="ErrorType.Forbidden"/> error.</summary>
    /// <param name="code">A stable, machine-readable identifier.</param>
    /// <param name="message">A human-readable explanation.</param>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>Returns <c>Type:Code</c>, without the message.</summary>
    public override string ToString() => $"{Type}:{Code}";
}
