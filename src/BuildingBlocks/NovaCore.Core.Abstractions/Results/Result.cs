namespace NovaCore.Core.Abstractions.Results;

/// <summary>
/// Outcome of an operation that can fail in expected ways. Zero dependencies, no exceptions
/// for control flow. Convert to an HTTP response once, at the edge (see Core.Infrastructure).
/// </summary>
public class Result
{
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

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}

/// <summary>Outcome of an operation that yields a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(true, Error.None) => _value = value;
    private Result(Error error) : base(false, error) => _value = default;

    /// <summary>The produced value. Throws if accessed on a failed result — check <see cref="Result.IsSuccess"/> first.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(Error error) => new(error);

    /// <summary>Lets handlers simply <c>return value;</c> for the happy path.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Lets handlers simply <c>return error;</c> for the failure path.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>Functional fold over both branches — handy when mapping to a transport response.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(Error);
}
