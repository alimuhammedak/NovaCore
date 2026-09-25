# CLAUDE.md — BuildingBlocks (the reusable core)

Subtree rules for `src/BuildingBlocks/`. See the repo-root `CLAUDE.md` for global standards.

These are the NuGet packages a NovaCore host builds on (docs/plan/ANALYSIS.md §4), so keep them
**module-agnostic**: nothing here may reference `NovaCore.Modules.*`.

## Packages — put new code in the lowest package that fits

**Core** (BCL and Microsoft packages only, D-003; enforced by `PackageDependencyTests`):

- **`NovaCore.Abstractions`** — BCL only. **No `FrameworkReference`, no NuGet, no ASP.NET, no EF.**
  Home of `Result`/`Result<T>`/`Error`, `PagedList<T>`/`PaginationRequest`, `Entity`/`AuditableEntity`,
  `IDomainEvent` (events derive from the `DomainEvent` record, which fixes `EventId`/`OccurredOnUtc` at
  creation), `ICacheService`, `ICurrentUser`. If a new type needs a framework, it does **not** belong here.

- **`NovaCore.AspNetCore`** — the web host: `IModule` + `AddModule<TModule>()`/`MapModules`, `AddCoreDefaults`
  (health + HttpClient resilience), `AddCoreWeb`/`UseCoreWeb`, `GlobalExceptionHandler` (RFC 9457) and the
  `AppException` types, `ResultHttpExtensions.ToHttpResult`, JWT auth (`AddCoreAuthentication`), permission authz
  (`Authorization/`), the OpenAPI document, `SecurityHeadersMiddleware`, `HybridCacheService`. Depends on
  Abstractions + `FrameworkReference Microsoft.AspNetCore.App`, never on EntityFrameworkCore.

- **`NovaCore.EntityFrameworkCore`** — persistence without ASP.NET Core: `AddModuleDbContext`, `AddCorePersistence`
  (`AuditableEntityInterceptor`, `DomainEventDispatcher`, opt-in `DatabaseMigrationService`). Depends on Abstractions,
  EF Core and the `Microsoft.Extensions` hosting/configuration abstractions only, never on AspNetCore (D-020).

**Add-ons** (optional, one third-party library each; they may reference AspNetCore/Abstractions only):
`NovaCore.Serilog` (`AddCoreLogging`), `NovaCore.OpenTelemetry` (`AddCoreObservability`), `NovaCore.Scalar`
(`MapCoreScalar`). A new third-party integration becomes a new add-on, not a dependency of the core.

**Not a package:** `NovaCore.FluentValidation` (`ValidationFilter<T>`, `ValidationExceptionHandler`,
`AddValidationExceptionHandler`) keeps FluentValidation out of the core until .NET 10 validation replaces it (D-019).

## Conventions here

- Public surface is **extension methods** that read as a two-call story: `builder.AddXxx()` then
  `app.UseXxx()`/`app.MapXxx()`. Keep `Program.cs` in consumers tiny.
- **`IExceptionHandler` order matters** — they run in registration order (R-046). `AddValidationExceptionHandler`
  inserts `ValidationExceptionHandler` ahead of `GlobalExceptionHandler` (validation wins); `ExceptionHandlerOrderTests`
  checks it.
- Caching goes through `ICacheService` (wraps HybridCache). Don't take a hard dependency on `HybridCache`
  in feature code.
- Permission authorization: the dynamic `PermissionPolicyProvider` maps any `perm:<name>` policy; expose
  ergonomics via `RequirePermission(...)` / `[HasPermission]` rather than hand-rolled `[Authorize(Roles=)]`.
- The packages are `IsPackable=true`; avoid leaking app-specific assumptions (connection strings,
  concrete modules) into them. A new package needs `GenerateDocumentationFile`, the PublicApiAnalyzers reference and
  the two `PublicAPI.*.txt` files; `Directory.Build.targets` fails the build without the first two.
- **They are NuGet packages, so their public surface is tracked:**
  - Every public member has an XML doc comment. The docs also appear in consumers' OpenAPI documents (R-041), so
    describe behaviour and keep the claims true to the code.
  - `PublicAPI.Unshipped.txt` must list every public API (RS0016/RS0017).
  - No two public overloads with optional parameters (RS0026). `ToHttpResult` has four plain overloads for this reason.
  - `Result<T>`'s own `Success`/`Failure` are internal (CA1000). Callers use `Result.Success<T>(…)`/`Result.Failure<T>(…)`
    or the implicit conversions.
