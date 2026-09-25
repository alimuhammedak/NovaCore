using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace NovaCore.AspNetCore.Security;

/// <summary>
/// Adds defensive HTTP response headers (clickjacking, MIME-sniffing, referrer leakage, cross-origin
/// isolation). A strict Content-Security-Policy is applied to API responses but skipped for the Scalar
/// docs UI, which legitimately loads its own assets.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>Registers the headers to be added when the response starts, then calls the next middleware.</summary>
    /// <param name="context">The current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            var headers = ctx.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            // Lock down API responses; leave the docs UI (Scalar) free to load its assets.
            var path = ctx.Request.Path;
            if (!path.StartsWithSegments("/scalar") && !path.StartsWithSegments("/openapi"))
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            return Task.CompletedTask;
        }, context);

        await next(context);
    }
}

/// <summary>Adds <see cref="SecurityHeadersMiddleware"/> to the pipeline.</summary>
public static class SecurityHeadersExtensions
{
    /// <summary>Adds <see cref="SecurityHeadersMiddleware"/> to the pipeline.</summary>
    /// <param name="app">The application builder.</param>
    /// <returns><paramref name="app"/>, for chaining.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
