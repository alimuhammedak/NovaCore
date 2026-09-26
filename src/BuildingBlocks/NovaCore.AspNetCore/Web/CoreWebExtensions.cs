using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NovaCore.Abstractions.Identity;
using NovaCore.AspNetCore.DependencyInjection;
using NovaCore.AspNetCore.Auth;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.AspNetCore.OpenApi;
using NovaCore.AspNetCore.Security;

namespace NovaCore.AspNetCore.Web;

/// <summary>
/// The web-layer composition root: ProblemDetails, exception handling, the OpenAPI document, JWT auth,
/// permission authorization, rate limiting and CORS — all behind two calls. The Scalar UI is the NovaCore.Scalar add-on.
/// </summary>
public static class CoreWebExtensions
{
    /// <summary>The name of the CORS policy registered by <see cref="AddCoreWeb"/>.</summary>
    public const string CorsPolicyName = "novacore";

    /// <summary>
    /// Registers the web services: ProblemDetails and the global exception handler, OpenAPI, JWT authentication,
    /// permission authorization, rate limiting and CORS.
    /// </summary>
    public static IHostApplicationBuilder AddCoreWeb(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Don't advertise the server software (defence in depth).
        services.Configure<KestrelServerOptions>(options => options.AddServerHeader = false);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddCoreInfrastructure(); // adds ProblemDetails + GlobalExceptionHandler + caching

        services.AddCoreAuthentication();
        services.AddCoreAuthorization(); // the handler behind RequirePermission(...)

        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        AddRateLimiting(services);
        AddCors(services);

        return builder;
    }

    /// <summary>Adds the middleware that pairs with <see cref="AddCoreWeb"/>, in the order it must run.</summary>
    public static WebApplication UseCoreWeb(this WebApplication app)
    {
        app.UseSecurityHeaders();            // defensive response headers first, so every response carries them
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        // HSTS in non-dev. (HTTPS redirection is intentionally left to the gateway/host so the in-memory
        // test server and plain-HTTP local runs keep working; enable UseHttpsRedirection at the edge.)
        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        // The OpenAPI document, in Development only. The Scalar UI over it is the NovaCore.Scalar add-on (MapCoreScalar).
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseCors(CorsPolicyName);
        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    // Configuration is read when the options are first built, not here: values layered in after CreateBuilder (e.g. by
    // WebApplicationFactory) still apply, and invalid values fail on start (docs/plan/RESEARCH.md R-048).
    private static void AddRateLimiting(IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<RateLimitingOptions>, RateLimitingOptionsValidator>());

        services.AddRateLimiter(options => options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);

        services.AddOptions<RateLimiterOptions>().Configure<IOptions<RateLimitingOptions>>((options, limits) =>
        {
            var permitLimit = limits.Value.PermitLimit;
            var window = TimeSpan.FromSeconds(limits.Value.WindowSeconds);

            // Global fixed-window limiter partitioned by caller IP.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = window,
                        QueueLimit = 0
                    }));

            // Opt-in stricter policy for sensitive endpoints: .RequireRateLimiting("strict"). Also partitioned by
            // caller IP: an unpartitioned limiter is one shared counter, so a single client could lock everyone out.
            options.AddPolicy("strict", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(1, permitLimit / 10),
                        Window = window,
                        QueueLimit = 0
                    }));
        });
    }

    private static void AddCors(IServiceCollection services)
    {
        services.AddOptions<CorsOriginsOptions>()
            .BindConfiguration(CorsOriginsOptions.SectionName)
            .Validate(
                cors => cors.AllowedOrigins.All(CorsOriginsOptions.IsOrigin),
                "Cors:AllowedOrigins entries must be origins such as https://app.example.com: scheme and host (and port), "
                + "no path, no trailing slash and no '*'.")
            .ValidateOnStart();

        services.AddCors();

        services.AddOptions<CorsOptions>().Configure<IOptions<CorsOriginsOptions>>((options, cors) =>
            options.AddPolicy(CorsPolicyName, policy =>
            {
                var origins = cors.Value.AllowedOrigins;
                if (origins.Length > 0)
                    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                else
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }));
    }
}
