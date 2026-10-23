using CsCheck;
using Microsoft.AspNetCore.Http.HttpResults;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Http;
using Xunit;

namespace NovaCore.Domain.Tests;

/// <summary>
/// <see cref="Result{T}"/> and its HTTP translation over generated values and errors (CsCheck): a result holds a value or
/// an error, never both, and every error type becomes a ProblemDetails response with the error's code and message.
/// </summary>
public sealed class ResultPropertyTests
{
    private static readonly Gen<Error> Failures = Gen.Select(
        Gen.String[Gen.Char.AlphaNumeric, 1, 20], Gen.String, Gen.Enum<ErrorType>(),
        (code, message, type) => new Error(code, message, type));

    [Fact]
    public void A_success_holds_its_value_and_no_error()
    {
        Gen.Int.Sample(value =>
        {
            var result = Result.Success(value);

            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal(Error.None, result.Error);
            Assert.Equal(value, result.Value);
            Assert.Equal(value, result.Match(success => success, _ => throw new InvalidOperationException()));
        });
    }

    [Fact]
    public void A_failure_holds_its_error_and_no_value()
    {
        Failures.Sample(error =>
        {
            var result = Result.Failure<int>(error);

            Assert.True(result.IsFailure);
            Assert.Equal(error, result.Error);
            Assert.Throws<InvalidOperationException>(() => _ = result.Value);
            Assert.Equal(error, result.Match(_ => throw new InvalidOperationException(), failure => failure));
        });
    }

    [Fact]
    public void Every_error_becomes_a_problem_with_its_status_code_and_message()
    {
        Failures.Sample(error =>
        {
            var response = Assert.IsType<ProblemHttpResult>(Result.Failure<int>(error).ToHttpResult().Result);

            Assert.Equal(Status(error.Type), response.StatusCode);
            Assert.Equal(error.Message, response.ProblemDetails.Detail);
            Assert.Equal(error.Code, response.ProblemDetails.Extensions["code"]);
        });
    }

    private static int Status(ErrorType type) => type switch
    {
        ErrorType.Validation => 400,
        ErrorType.Unauthorized => 401,
        ErrorType.Forbidden => 403,
        ErrorType.NotFound => 404,
        ErrorType.Conflict => 409,
        _ => 500
    };
}
