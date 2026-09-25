using NovaCore.AspNetCore.Hosting;
using NovaCore.AspNetCore.Modules;
using NovaCore.AspNetCore.Web;
using NovaCore.EntityFrameworkCore.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreDefaults();   // structured logging (Serilog) + OpenTelemetry + health + resilient HttpClients
builder.AddCoreWeb();        // ProblemDetails, OpenAPI + Scalar, JWT auth, rate limiting, CORS
builder.AddCorePersistence(); // audit interceptor, domain events, opt-in startup migrations (before the modules)
builder.AddModules();        // discover every NovaCore.Modules.* IModule and register its services

var app = builder.Build();

app.UseCoreWeb();            // exception handling, auth, rate limiter, Scalar UI (Development)
app.MapModules();            // map every module's endpoints
app.MapDefaultEndpoints();   // /health (readiness) and /alive (liveness)

app.Run();

// Exposed so the integration test project can spin up the real host via WebApplicationFactory.
public partial class Program;
