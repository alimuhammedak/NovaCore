# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- MIT `LICENSE` and this changelog.
- Architecture tests for the testable bans in `docs/plan/DECISIONS.md` (banned packages, module boundaries, repository
  and helper types, DI scope validation, domain types in endpoint signatures, TODO/FIXME in `src/`).
- GitHub Actions CI (`.github/workflows/ci.yml`): restore with NuGet audit, Release build, tests, EF Core
  pending-model-changes check for the three contexts, and a vulnerable-package check.
- NuGet package metadata for the four packable projects: MIT license expression, a shared package README
  (`src/PACKAGE.md`), repository URL, XML documentation, `.snupkg` symbol packages and package validation. Version
  `0.1.0` (`VersionPrefix`).
- XML documentation for every public member of `NovaCore.Core.Abstractions`, `NovaCore.Core.Infrastructure` and
  `NovaCore.Core.Web`.
- Public API tracking with `Microsoft.CodeAnalysis.PublicApiAnalyzers` (`PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt`
  per package).
- Release workflow (`.github/workflows/release.yml`): on a `v*` tag matching `VersionPrefix`, packs and pushes to
  GitHub Packages.
- Architecture tests for the package map: the core packages depend on Microsoft packages only and point one way
  (Abstractions ← AspNetCore / EntityFrameworkCore, add-ons → AspNetCore), and the building blocks never scan
  assemblies. Host wiring tests pin the module order and endpoints, the exception-handler order and the Scalar UI.
- `Directory.Build.targets` fails the build of a packable project that does not reference PublicApiAnalyzers.

### Changed

- Every project builds with the `latest-recommended` analyzers, `EnforceCodeStyleInBuild` and `TreatWarningsAsErrors`.
  Low and moderate NuGet advisories (NU1901/NU1902) stay warnings.
- **Breaking:** `Result<T>.Success(T)` and `Result<T>.Failure(Error)` are no longer public. Use `Result.Success<T>(value)`,
  `Result.Failure<T>(error)` or the implicit conversions. Note that `Result<T>.Failure(error)` still compiles, but it now
  binds to the inherited `Result.Failure(Error)` and returns a non-generic `Result`.
- **Breaking (binary):** `ResultHttpExtensions.ToHttpResult` no longer has optional parameters. It has four overloads:
  `(Result)`, `(Result, int)`, `(Result<T>)`, `(Result<T>, Func<T, IResult>?)`. Existing source compiles and behaves
  unchanged.
- The OpenAPI document now includes descriptions taken from the XML docs of the packable projects (for example on the
  `PagedList` schemas). .NET 10's OpenAPI source generator picks them up automatically.
- **Breaking:** the packages are restructured to the target package map (docs/plan/ANALYSIS.md §4). Package names and
  namespace roots change; the members keep their names and behaviour:
  - `NovaCore.Core.Abstractions` → `NovaCore.Abstractions`.
  - New `NovaCore.EntityFrameworkCore` (Microsoft packages only, no ASP.NET Core): `AddModuleDbContext`,
    `AuditableEntityInterceptor`, `DatabaseMigrationService` and `DomainEventDispatcher`, moved out of
    `NovaCore.Core.Infrastructure`. Hosts register them with the new `builder.AddCorePersistence()`, called before the
    modules; `AddCoreWeb`/`AddCoreInfrastructure` no longer do.
  - `NovaCore.Core.Web` and the rest of `NovaCore.Core.Infrastructure` (ProblemDetails exception handling, the
    `AppException` types, `ResultHttpExtensions`, `HybridCacheService`) → `NovaCore.AspNetCore`. The
    `NovaCore.Core.Infrastructure` package no longer exists.
  - New add-on `NovaCore.Serilog`: `AddCoreLogging` moved here from `AddCoreDefaults`, which no longer configures
    logging. `UseCoreWeb` no longer calls `UseSerilogRequestLogging`; the host calls it before `UseCoreWeb`.
  - New add-on `NovaCore.OpenTelemetry`: `AddCoreObservability` moved here from `AddCoreDefaults`, which now only adds
    health checks and the HttpClient resilience handler.
  - New add-on `NovaCore.Scalar`: `app.MapCoreScalar()` maps the Scalar UI in Development. `UseCoreWeb` still maps the
    OpenAPI document but no longer the Scalar UI.
  - `ValidationFilter<T>` and `ValidationExceptionHandler` (FluentValidation) moved to the non-packable
    `NovaCore.FluentValidation` project, so the core packages depend on Microsoft packages only. `AddCoreWeb` no longer
    registers the handler; the host calls `AddValidationExceptionHandler()`, which puts it ahead of the global handler.
    .NET 10's built-in validation replaces this project in a later phase.
- **Breaking:** modules are registered explicitly with `builder.AddModule<TModule>()` and mapped in that order.
  `AddModules()` and the assembly-scanning `ModuleLoader` are removed, and so is `IModule.IsEnabled` (it was never
  configurable): leave a module out by not adding it.
  Adding the same module twice throws at registration.
- `AddModuleDbContext` also registers `AuditableEntityInterceptor` (if not already registered), so a module's context
  resolves even before the host calls `AddCorePersistence`.
  - `NovaCore.Modules.Catalog.Contracts` is no longer packed. The Catalog module is sample code; only the building
    blocks are NuGet packages.
- Logging goes through source-generated `[LoggerMessage]` methods (`GlobalExceptionHandler`, `DatabaseMigrationService`,
  the Identity security audit log, seeder and development email sender). Levels and message templates are unchanged;
  every entry now carries an explicit event ID and event name.
- Configuration goes through the options pattern and is validated when the host starts. `Jwt`, `RateLimiting`, `Cors`,
  `Cache` and `Database` are read when first used rather than at registration, so configuration added after `CreateBuilder` (for
  example by `WebApplicationFactory`) now applies. Invalid values stop startup with a message naming the setting:
  - a missing `Jwt:SigningKey` outside Development, or one shorter than 32 bytes;
  - an empty `Jwt:Issuer`/`Jwt:Audience`, or non-positive token lifetimes;
  - a non-positive `RateLimiting:PermitLimit`/`WindowSeconds` or `Cache:DefaultExpiration`;
  - a `Cors:AllowedOrigins` entry that is not a bare origin (`*`, or one with a path or trailing slash);
  - a `Database:ApplyMigrations` value that is not a boolean.
- **Breaking:** `JwtOptions` is registered as `IOptions<JwtOptions>` instead of a plain singleton, and
  `JwtTokenService` takes `IOptions<JwtOptions>`. `AddCoreAuthentication`, `AddCoreInfrastructure` and `AddCoreCaching`
  no longer take `IConfiguration` (nor `IHostEnvironment`); they read both from DI.
- The time comes from an injected `TimeProvider` (`TimeProvider.System` unless the host registers another):
  audit stamps, access-token `iat`/`exp`, refresh-token creation, expiry and revocation, user creation and the admin
  lockout listing. Entities no longer default `CreatedAtUtc` to the current time; the code that creates them sets it.
- **Breaking:** `AccessToken` carries `IssuedAtUtc`, and `ExpiresInSeconds` is the token's lifetime
  (`ExpiresAtUtc - IssuedAtUtc`) instead of the time left when read. The token response's `expiresIn` is now the full
  lifetime (e.g. 900 instead of 899). `JwtTokenService` and `AuditableEntityInterceptor` take a `TimeProvider`.
- **Breaking:** `DatabaseMigrationService` is internal. `AddCorePersistence` always registers it and it reads
  `Database:ApplyMigrations` when the host starts. `NovaCore.EntityFrameworkCore` references
  `Microsoft.Extensions.Options.ConfigurationExtensions` instead of `Microsoft.Extensions.Configuration.Binder` (which
  still comes transitively).

### Removed

- Unused dependencies and wiring: `Scrutor` (never called), `Asp.Versioning.Http` + `AddApiVersioning` (no endpoint used a
  version set), `AddOutputCache`/`UseOutputCache` (no policy or endpoint used it).
- **Breaking:** `PagedList<T>.Empty(int)` (it had no callers).

### Security

- `docker-compose.yml` reads the SQL Server SA password from a git-ignored `.env` file instead of a hard-coded value.
- Fixed two high-severity transitive vulnerabilities by updating the top-level packages: `Microsoft.OpenApi` 2.0.0
  ([GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc)) and `SQLitePCLRaw.lib.e_sqlite3` 2.1.11
  ([GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)). ASP.NET Core and EF Core packages → 10.0.12,
  OpenTelemetry → 1.19.x, Scalar → 2.17.9.
- NuGet audit: high (NU1903) and critical (NU1904) vulnerabilities now fail the restore.
- `LoggingEmailSender`, which writes email bodies (including confirmation tokens) to the log, is registered only in
  Development. In any other environment the host fails at startup with a clear error unless a real `IEmailSender` is
  registered.
- `GlobalExceptionHandler` sends only `AppException` messages to the client. Other exceptions it maps to a 4xx status
  (`ArgumentException`, `FormatException`, `UnauthorizedAccessException`, `KeyNotFoundException`) keep their status
  code, but their message (which can carry connection strings, paths or other internals) is only logged.

### Fixed

- The ProblemDetails `instance` member is a URI reference, the request path (e.g. `/api/v1/products/42`), instead of
  `"GET /api/v1/products/42"`. The query string is left out.

- The `"strict"` rate-limit policy (used by `/api/v1/auth`) is now partitioned per client IP. Before, it was a single
  shared counter, so one client could exhaust it and lock every other client out of login.
- `IDomainEvent.EventId` and `OccurredOnUtc` returned a new value on every read (default interface members). They are
  now plain interface properties, and the new `DomainEvent` base record fixes both when the event is created.
  **Breaking:** events must derive from `DomainEvent` (or implement the two properties themselves).
