using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Exceptions;
using NovaCore.FluentValidation;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// Exception handlers run in registration order (docs/plan/RESEARCH.md R-046). The FluentValidation handler must come
/// before the catch-all, or a validation exception becomes a generic 400 without the field errors.
/// </summary>
public class ExceptionHandlerOrderTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>
{
    private readonly NovaCoreApiFactory _factory = factory;

    [Fact]
    public void Validation_exception_handler_runs_before_the_global_handler()
    {
        var handlers = _factory.Services.GetServices<IExceptionHandler>().Select(handler => handler.GetType()).ToList();

        Assert.Equal([typeof(ValidationExceptionHandler), typeof(GlobalExceptionHandler)], handlers);
    }
}
