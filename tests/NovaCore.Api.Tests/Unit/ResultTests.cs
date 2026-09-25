using NovaCore.Abstractions.Results;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

public class ResultTests
{
    [Fact]
    public void Success_carries_value_and_no_error()
    {
        Result<int> result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_carries_error_and_blocks_value_access()
    {
        var error = Error.NotFound("x.not_found", "missing");
        Result<int> result = Result.Failure<int>(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Implicit_conversion_from_value_yields_success()
    {
        Result<string> result = "hello";

        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Implicit_conversion_from_error_yields_failure()
    {
        Result<string> result = Error.Validation("x.invalid", "bad");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Match_folds_both_branches()
    {
        Result<int> success = Result.Success(10);
        Result<int> failure = Result.Failure<int>(Error.Conflict("c", "conflict"));

        Assert.Equal("ok:10", success.Match(v => $"ok:{v}", e => $"err:{e.Code}"));
        Assert.Equal("err:c", failure.Match(v => $"ok:{v}", e => $"err:{e.Code}"));
    }

    [Fact]
    public void Constructing_success_with_an_error_is_an_invariant_violation()
    {
        Assert.Throws<InvalidOperationException>(() => new BrokenResult());
    }

    // Success must carry Error.None; passing a real error must throw.
    private sealed class BrokenResult() : Result(true, new Error("oops", "should not happen"));
}
