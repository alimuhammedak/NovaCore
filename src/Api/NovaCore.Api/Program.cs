using NovaCore.Core.Web.Hosting;
using NovaCore.Core.Web.Modules;
using NovaCore.Core.Web.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreDefaults();   // structured logging (Serilog) + OpenTelemetry + health + resilient HttpClients
builder.AddCoreWeb();        // ProblemDetails, OpenAPI + Scalar, JWT auth, rate limiting, CORS
builder.AddModules();        // discover every NovaCore.Modules.* IModule and register its services

var app = builder.Build();

app.UseCoreWeb();            // exception handling, auth, rate limiter, Scalar UI (Development)
app.MapModules();            // map every module's endpoints
app.MapDefaultEndpoints();   // /health (readiness) and /alive (liveness)

app.Run();

// Exposed so the integration test project can spin up the real host via WebApplicationFactory.
public partial class Program;
