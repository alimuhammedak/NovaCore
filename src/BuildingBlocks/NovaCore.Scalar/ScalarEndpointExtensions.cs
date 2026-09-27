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
    public static WebApplication MapCoreScalar(this WebApplication app) => app.MapCoreScalar(_ => { });

    /// <summary>
    /// In Development, maps the Scalar UI like <see cref="MapCoreScalar(WebApplication)"/>, then lets
    /// <paramref name="configure"/> adjust it, e.g. preselect a security scheme and fill in its OAuth client. What it sets
    /// is visible in the browser, which is why the UI exists in Development only.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="configure">Adjusts the Scalar options after NovaCore's defaults.</param>
    /// <returns><paramref name="app"/>, for chaining.</returns>
    public static WebApplication MapCoreScalar(this WebApplication app, Action<ScalarOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("NovaCore API").WithTheme(ScalarTheme.Mars);
                configure(options);
            });
        }

        return app;
    }
}
