using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NovaCore.Core.Abstractions.Identity;
using NovaCore.Core.Infrastructure.DependencyInjection;
using NovaCore.Core.Infrastructure.Persistence;
using NovaCore.Core.Web.Auth;
using NovaCore.Core.Web.Authorization;
using NovaCore.Core.Web.Identity;
using NovaCore.Core.Web.OpenApi;
using NovaCore.Core.Web.Security;
using NovaCore.Core.Web.Validation;
using Scalar.AspNetCore;
using Serilog;

namespace NovaCore.Core.Web.Web;

/// <summary>
/// The web-layer composition root: ProblemDetails, exception handling, OpenAPI + Scalar, JWT auth,
/// rate limiting and CORS — all behind two calls.
/// </summary>
public static class CoreWebExtensions
{
    public const string CorsPolicyName = "novacore";

    public static IHostApplicationBuilder AddCoreWeb(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Don't advertise the server software (defence in depth).
        services.Configure<KestrelServerOptions>(options => options.AddServerHeader = false);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        // Exception handlers run in registration order: validation first, catch-all last.
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddCoreInfrastructure(builder.Configuration); // adds ProblemDetails + GlobalExceptionHandler + caching

        services.AddCoreAuthentication(builder.Configuration, builder.Environment);
        services.AddCoreAuthorization(); // permission-based policy provider + handler

        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        AddRateLimiting(services, builder.Configuration);

        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            if (origins.Length > 0)
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            else
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }));

        // Opt-in: apply every module's EF Core migrations on startup. Off by default so test hosts
        // and DB-less runs never block. Enable with Database:ApplyMigrations=true once SQL Server is reachable.
        if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
            services.AddHostedService<DatabaseMigrationService>();

        return builder;
    }

    public static WebApplication UseCoreWeb(this WebApplication app)
    {
        app.UseSecurityHeaders();            // defensive response headers first, so every response carries them
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        // HSTS in non-dev. (HTTPS redirection is intentionally left to the gateway/host so the in-memory
        // test server and plain-HTTP local runs keep working; enable UseHttpsRedirection at the edge.)
        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options => options
                .WithTitle("NovaCore API")
                .WithTheme(ScalarTheme.Mars));
        }

        app.UseCors(CorsPolicyName);
        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue<int?>("RateLimiting:PermitLimit") ?? 100;
        var windowSeconds = configuration.GetValue<int?>("RateLimiting:WindowSeconds") ?? 60;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global fixed-window limiter partitioned by caller IP.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
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
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0
                    }));
        });
    }
}
