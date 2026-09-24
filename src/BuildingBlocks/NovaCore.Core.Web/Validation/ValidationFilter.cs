using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace NovaCore.Core.Web.Validation;

/// <summary>
/// Endpoint filter that runs the registered FluentValidation <see cref="IValidator{T}"/> for the
/// request DTO before the handler executes. If no validator is registered it is a no-op, so it is
/// always safe to attach. Invalid input becomes an RFC 9457 validation ProblemDetails (400).
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
            return await next(context);

        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
            return await next(context);

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (result.IsValid)
            return await next(context);

        return Results.ValidationProblem(
            result.ToDictionary(),
            title: "Validation failed",
            statusCode: StatusCodes.Status400BadRequest);
    }
}
