namespace NovaCore.Abstractions.Results;

/// <summary>
/// Outcome of an operation that can fail in expected ways. Zero dependencies, no exceptions
/// for control flow. Convert to an HTTP response once, at the edge (see Core.Infrastructure).
/// </summary>
public class Result
{
    /// <summary>Creates a result. Use the <c>Success</c>/<c>Failure</c> factories instead of calling this directly.</summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The failure reason; must be <see cref="Error.None"/> on success and anything else on failure.</param>
    /// <exception cref="InvalidOperationException"><paramref name="isSuccess"/> and <paramref name="error"/> disagree.</exception>
    protected Result(bool isSuccess, Error error)
    {
        // Guard the invariant: success must carry no error; failure must carry one.
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot contain an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must contain an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>True when the operation failed; <see cref="Error"/> then says why.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Why the operation failed, or <see cref="Error.None"/> on success.</summary>
    public Error Error { get; }

    /// <summary>A successful result with no value.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>A failed result.</summary>
    /// <param name="error">Why the operation failed. Must not be <see cref="Error.None"/>.</param>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>A successful result carrying <paramref name="value"/>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value the operation produced.</param>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>A failed result for an operation that would have produced a <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of the value the operation would have produced.</typeparam>
    /// <param name="error">Why the operation failed. Must not be <see cref="Error.None"/>.</param>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}

/// <summary>Outcome of an operation that yields a value on success.</summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(true, Error.None) => _value = value;
    private Result(Error error) : base(false, error) => _value = default;

    /// <summary>The produced value. Throws if accessed on a failed result — check <see cref="Result.IsSuccess"/> first.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    // Internal: callers use Result.Success<T>/Result.Failure<T> or the implicit conversions, so a type argument never has
    // to be spelled out on the generic type (CA1000, docs/plan/DECISIONS.md D-012).
    internal static Result<T> Success(T value) => new(value);
    internal static new Result<T> Failure(Error error) => new(error);

    /// <summary>Lets handlers simply <c>return value;</c> for the happy path.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Lets handlers simply <c>return error;</c> for the failure path.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>Functional fold over both branches — handy when mapping to a transport response.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(Error);
}
