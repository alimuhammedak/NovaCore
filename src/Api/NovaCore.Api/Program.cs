using Microsoft.EntityFrameworkCore;
using NovaCore.AspNetCore.Hosting;
using NovaCore.AspNetCore.Modules;
using NovaCore.AspNetCore.Web;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Identity.OpenIddict;
using NovaCore.Modules.Catalog;
using NovaCore.Modules.Payments;
using NovaCore.OpenTelemetry;
using NovaCore.Scalar;
using NovaCore.Serilog;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add-ons (third-party packages): logging and observability.
builder.AddCoreLogging();                          // Serilog: console, plus Seq when configured
builder.AddCoreObservability();                    // OpenTelemetry metrics + tracing, OTLP export when configured

// Core.
builder.AddCoreDefaults();                         // health checks + resilient HttpClients
builder.AddCoreWeb();                              // ProblemDetails, OpenAPI, JWT auth, rate limiting, CORS
builder.AddCorePersistence();                      // audit interceptor, domain events, opt-in startup migrations

// The OpenID Connect provider (add-on), issuing the tokens Jwt:Authority points at. Its store lives in this host: the
// provider and the migrations are the host's choice. After AddCorePersistence, so migrations run before its seeding.
builder.AddCoreIdentityServer();
builder.Services.AddModuleDbContext<IdentityServerDbContext>((services, db) => db
    .UseSqlServer(
        services.GetRequiredService<IConfiguration>().GetConnectionString("Database"),
        sql => sql
            .MigrationsAssembly(typeof(Program).Assembly.GetName().Name)
            .MigrationsHistoryTable("__EFMigrationsHistory", IdentityServerDbContext.Schema)
            .EnableRetryOnFailure())
    .AddInterceptors(services.GetRequiredService<AuditableEntityInterceptor>()));

// Modules, after AddCorePersistence so migrations start before module hosted services. Endpoints map in this order.
builder.AddModule<CatalogModule>();
builder.AddModule<PaymentsModule>();

var app = builder.Build();

app.UseSerilogRequestLogging();                    // one log line per request, wrapping the rest of the pipeline
app.UseCoreWeb();                                  // security headers, exception handling, auth, rate limiter, OpenAPI (Development)
app.MapCoreScalar();                               // Scalar UI at /scalar/v1 (Development)
app.MapCoreIdentityServer();                       // /connect/*, the sign-in page, /api/v1/auth/* and /api/v1/admin/*
app.MapModules();                                  // every module's endpoints
app.MapDefaultEndpoints();                         // /health (readiness) and /alive (liveness), Development only

app.Run();

// Exposed so the integration test project can spin up the real host via WebApplicationFactory.
public partial class Program;
