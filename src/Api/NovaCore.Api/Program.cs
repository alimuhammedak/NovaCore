using NovaCore.AspNetCore.Hosting;
using NovaCore.AspNetCore.Modules;
using NovaCore.AspNetCore.Web;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Serilog;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreLogging();    // structured logging (Serilog: console, Seq when configured)
builder.AddCoreDefaults();   // OpenTelemetry + health + resilient HttpClients
builder.AddCoreWeb();        // ProblemDetails, OpenAPI + Scalar, JWT auth, rate limiting, CORS
builder.AddCorePersistence(); // audit interceptor, domain events, opt-in startup migrations (before the modules)
builder.AddModules();        // discover every NovaCore.Modules.* IModule and register its services

var app = builder.Build();

app.UseSerilogRequestLogging(); // one log line per request, wrapping the rest of the pipeline
app.UseCoreWeb();            // exception handling, auth, rate limiter, Scalar UI (Development)
app.MapModules();            // map every module's endpoints
app.MapDefaultEndpoints();   // /health (readiness) and /alive (liveness)

app.Run();

// Exposed so the integration test project can spin up the real host via WebApplicationFactory.
public partial class Program;
