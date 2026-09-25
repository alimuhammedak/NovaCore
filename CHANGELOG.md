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
- Logging goes through source-generated `[LoggerMessage]` methods (`GlobalExceptionHandler`, `DatabaseMigrationService`,
  the Identity security audit log, seeder and development email sender). Levels and message templates are unchanged;
  every entry now carries an explicit event ID and event name.

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

### Fixed

- The `"strict"` rate-limit policy (used by `/api/v1/auth`) is now partitioned per client IP. Before, it was a single
  shared counter, so one client could exhaust it and lock every other client out of login.
- `IDomainEvent.EventId` and `OccurredOnUtc` returned a new value on every read (default interface members). They are
  now plain interface properties, and the new `DomainEvent` base record fixes both when the event is created.
  **Breaking:** events must derive from `DomainEvent` (or implement the two properties themselves).
