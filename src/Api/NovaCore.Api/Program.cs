using Microsoft.EntityFrameworkCore;
using NovaCore.AspNetCore.Hosting;
using NovaCore.AspNetCore.Modules;
using NovaCore.AspNetCore.Web;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Identity.OpenIddict;
using NovaCore.Modules.Catalog;
using NovaCore.Modules.Payments;
using NovaCore.MultiTenancy;
using NovaCore.OpenTelemetry;
using NovaCore.Scalar;
using NovaCore.Serilog;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add-ons (third-party packages): logging and observability.
builder.AddCoreLogging();                          // Serilog: console, plus Seq when configured
builder.AddCoreObservability();                    // OpenTelemetry metrics + tracing, OTLP export when configured

// Core.
builder.AddCoreDefaults();                         // health checks + resilient HttpClients
builder.AddCoreWeb();                              // ProblemDetails, OpenAPI, JWT auth, rate limiting, CORS
builder.AddCorePersistence();                      // audit interceptor, domain events + outbox dispatcher, opt-in startup migrations

// The database of every module context, chosen here and nowhere else. Their SQL Server migrations live in this host
// (Catalog/, Payments/, Identity/), as do the design-time factories `dotnet ef` uses.
builder.Services.AddModuleDatabase((services, db) => db.UseSqlServer(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Database"),
    sql => sql.MigrationsAssembly(typeof(Program).Assembly).EnableRetryOnFailure()));

// The OpenID Connect provider (add-on), issuing the tokens Jwt:Authority points at. Its store is a context of this
// host's database. After AddCorePersistence, so migrations run before its seeding.
builder.AddCoreIdentityServer();
builder.Services.AddModuleDbContext<IdentityServerDbContext>(IdentityServerDbContext.Schema,
    (services, db) => db.AddInterceptors(services.GetRequiredService<AuditableEntityInterceptor>()));

// Multi-tenancy (add-on): a request under /api/v1/tenants/{tenant} works in that tenant only when the caller is a member
// of it and it is active. Its store is a context of this host's database. No module here has tenant entities.
builder.AddCoreMultiTenancy();
builder.Services.AddModuleDbContext<MultiTenancyDbContext>(MultiTenancyDbContext.Schema, (_, _) => { });

// Modules, after AddCorePersistence so migrations start before module hosted services. Endpoints map in this order.
builder.AddModule<CatalogModule>();
builder.AddModule<PaymentsModule>();

var app = builder.Build();

app.UseSerilogRequestLogging();                    // one log line per request, wrapping the rest of the pipeline
app.UseCoreWeb();                                  // security headers, exception handling, auth, rate limiter, OpenAPI (Development)
app.MapCoreScalar(scalar => scalar                // Scalar UI at /scalar/v1 (Development), signing in through
    .AddPreferredSecuritySchemes(IdentityServerHostingExtensions.OpenApiSecuritySchemeId)   // the host's own provider
    .AddAuthorizationCodeFlow(IdentityServerHostingExtensions.OpenApiSecuritySchemeId, flow =>
    {
        flow.ClientId = "scalar";                  // Identity:Clients in appsettings.Development.json
        flow.Pkce = Pkce.Sha256;
        flow.SelectedScopes = ["openid", "profile", "email", "roles"];
    }));
app.MapCoreIdentityServer();                       // /connect/*, the sign-in page, /api/v1/auth/* and /api/v1/admin/*
app.MapModules();                                  // every module's endpoints
app.MapDefaultEndpoints();                         // /health (readiness) and /alive (liveness), Development only

app.Run();

// Exposed so the integration test project can spin up the real host via WebApplicationFactory.
public partial class Program;
