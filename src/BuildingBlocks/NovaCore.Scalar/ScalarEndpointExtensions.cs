using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace NovaCore.Scalar;

/// <summary>Maps the Scalar API reference UI.</summary>
public static class ScalarEndpointExtensions
{
    /// <summary>
    /// In Development, maps the Scalar UI at <c>/scalar/v1</c> over the OpenAPI document that <c>UseCoreWeb</c> maps at
    /// <c>/openapi/v1.json</c>. Does nothing in other environments.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns><paramref name="app"/>, for chaining.</returns>
    public static WebApplication MapCoreScalar(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference(options => options
                .WithTitle("NovaCore API")
                .WithTheme(ScalarTheme.Mars));
        }

        return app;
    }
}
