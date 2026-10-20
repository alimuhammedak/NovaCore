# NovaCore

Building blocks for .NET 10 modular-monolith Web APIs. The core packages depend on the BCL and Microsoft packages
only; third-party integrations are separate, optional add-ons.

| Package | Contents |
|---|---|
| `NovaCore.Abstractions` | `Result`/`Error`, paging, base entities, domain-event and current-user abstractions (BCL only) |
| `NovaCore.AspNetCore` | Modules (`AddModule<T>`), ProblemDetails exception handling, `Result` → HTTP, OpenAPI document, JWT authentication, permission authorization, rate limiting, security headers |
| `NovaCore.EntityFrameworkCore` | Module-owned `DbContext`s, audit stamping, startup migrations, in-process domain events |
| `NovaCore.Serilog` | Add-on: Serilog logging (console, Seq) |
| `NovaCore.OpenTelemetry` | Add-on: OpenTelemetry logs, metrics and tracing, OTLP export |
| `NovaCore.Scalar` | Add-on: Scalar API reference UI (Development) |
| `NovaCore.Identity.OpenIddict` | Add-on: OpenID Connect provider (ASP.NET Core Identity + OpenIddict) |

> **Pre-release (0.x).** The public API is not stable yet. Package names and namespaces are expected to change before
> 1.0, and a 0.x minor version may contain breaking changes.

Source, documentation and issues: https://github.com/alimuhammedak/NovaCore

Licensed under the MIT License.
