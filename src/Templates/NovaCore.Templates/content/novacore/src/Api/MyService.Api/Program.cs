using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MyService.Modules.Todos;
using NovaCore.AspNetCore.Hosting;
using NovaCore.AspNetCore.Modules;
using NovaCore.AspNetCore.Web;
using NovaCore.EntityFrameworkCore.Persistence;
#if (BuiltInAuth)
using NovaCore.Identity.OpenIddict;
#endif
using NovaCore.OpenTelemetry;
using NovaCore.Scalar;
#if (BuiltInAuth)
using Scalar.AspNetCore;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreObservability();                    // logs, metrics and traces through OpenTelemetry; OTLP when OTEL_EXPORTER_OTLP_ENDPOINT is set
builder.AddCoreDefaults();                         // health checks + resilient HttpClients
builder.AddCoreWeb();                              // ProblemDetails, OpenAPI, JWT auth (Jwt:Authority), permissions, rate limiting, CORS
builder.AddCorePersistence();                      // audit stamping, the outbox dispatcher, opt-in startup migrations

// The database of every module context, chosen here and nowhere else. Their migrations live in this project (one folder
// per context), as does what `dotnet ef` builds them with (DesignTimeDatabase.cs): change the two together.
#if (DbSqlServer)
builder.Services.AddModuleDatabase((services, db) => db.UseSqlServer(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Database"),
    sql => sql.MigrationsAssembly(typeof(Program).Assembly).EnableRetryOnFailure()));
#else
builder.Services.AddModuleDatabase((services, db) => db.UseNpgsql(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Database"),
    npgsql => npgsql.MigrationsAssembly(typeof(Program).Assembly).EnableRetryOnFailure()));
#endif

#if (BuiltInAuth)
// The OpenID Connect provider issuing the tokens Jwt:Authority points at: this host. Its store is a context of the
// database above, with its migrations in Identity/Migrations. After AddCorePersistence, so migrations run before seeding.
builder.AddCoreIdentityServer();
builder.Services.AddModuleDbContext<IdentityServerDbContext>(IdentityServerDbContext.Schema,
    (services, db) => db.AddInterceptors(services.GetRequiredService<AuditableEntityInterceptor>()));
#else
// The tokens come from an external OpenID Connect provider (Jwt:Authority). Permissions are resolved on the server, never
// read from the token: register the IPermissionResolver that answers them for that provider's users, e.g.
// builder.Services.AddScoped<IPermissionResolver, YourPermissionResolver>(). The host doesn't start without one.
#endif

// Modules, after AddCorePersistence. Their endpoints map in this order.
builder.AddModule<TodosModule>();

var app = builder.Build();

// Probes outside Development (where MapDefaultEndpoints maps them below): only on the management port in
// HealthChecks:ProbePort, which the host must also listen on. The check is on the connection's local port, which a client
// can't spoof the way it can the Host header that RequireHost reads. Before UseCoreWeb, which requires a signed-in caller.
if (!app.Environment.IsDevelopment() && app.Configuration.GetValue<int?>("HealthChecks:ProbePort") is { } probePort)
{
    app.UseHealthChecks("/health", probePort, new HealthCheckOptions());
    app.UseHealthChecks("/alive", probePort, new HealthCheckOptions { Predicate = check => check.Tags.Contains("live") });
}

app.UseCoreWeb();                                  // forwarded headers, security headers, exception handling, auth, rate limiter
#if (BuiltInAuth)
app.MapCoreScalar(scalar => scalar                // Scalar UI at /scalar/v1 (Development), signing in through this host
    .AddPreferredSecuritySchemes(IdentityServerHostingExtensions.OpenApiSecuritySchemeId)
    .AddAuthorizationCodeFlow(IdentityServerHostingExtensions.OpenApiSecuritySchemeId, flow =>
    {
        flow.ClientId = "scalar";                  // Identity:Clients in appsettings.Development.json
        flow.Pkce = Pkce.Sha256;
        flow.SelectedScopes = ["openid", "profile", "email", "roles"];
    }));
app.MapCoreIdentityServer();                       // /connect/*, the sign-in pages, /api/v1/auth/* and /api/v1/admin/*
#else
app.MapCoreScalar();                               // Scalar UI at /scalar/v1 (Development)
#endif
app.MapModules();                                  // every module's endpoints
app.MapDefaultEndpoints();                         // /health and /alive, Development only

app.Run();

// Exposed so the integration tests can start the real host through WebApplicationFactory.
public partial class Program;
