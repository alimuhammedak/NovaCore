using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace NovaCore.FluentValidation;

/// <summary>Service registration for the FluentValidation bridge.</summary>
public static class ValidationServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ValidationExceptionHandler"/> ahead of every other <see cref="IExceptionHandler"/>. Handlers
    /// run in registration order (docs/plan/RESEARCH.md R-046), so inserting it first keeps it in front of
    /// <c>GlobalExceptionHandler</c> wherever the host calls this.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddValidationExceptionHandler(this IServiceCollection services)
    {
        // The same singleton registration AddExceptionHandler<T> makes, at the front of the list.
        services.Insert(0, ServiceDescriptor.Singleton<IExceptionHandler, ValidationExceptionHandler>());
        return services;
    }
}
