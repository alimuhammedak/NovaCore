# CLAUDE.md — BuildingBlocks (the reusable core)

Subtree rules for `src/BuildingBlocks/`. See the repo-root `CLAUDE.md` for global standards.

The core is what a `dotnet new novacore` project inherits and what (optionally) ships as NuGet packages,
so keep it **module-agnostic**: nothing here may reference `NovaCore.Modules.*`.

## Layer responsibilities — put new code in the lowest layer that fits

- **`NovaCore.Core.Abstractions`** — BCL only. **No `FrameworkReference`, no NuGet, no ASP.NET, no EF.**
  Home of `Result`/`Result<T>`/`Error`, `PagedList<T>`/`PaginationRequest`, `Entity`/`AuditableEntity`,
  `IDomainEvent` (events derive from the `DomainEvent` record, which fixes `EventId`/`OccurredOnUtc` at
  creation), `ICacheService`, `ICurrentUser`. The "stays framework-free" rule is enforced by an
  architecture test — if a new type needs the framework, it does **not** belong here.

- **`NovaCore.Core.Infrastructure`** — cross-cutting *implementations* that need the ASP.NET/EF surface
  but not the web host: `GlobalExceptionHandler` (RFC 9457), `ResultHttpExtensions.ToHttpResult`,
  `HybridCacheService`, `AuditableEntityInterceptor`, `AddModuleDbContext` + `DatabaseMigrationService`,
  `DomainEventDispatcher`. Depends on Core.Abstractions + `FrameworkReference Microsoft.AspNetCore.App`.

- **`NovaCore.Core.Web`** — host composition: `IModule` + `ModuleLoader`, `AddCoreDefaults`
  (Serilog/OTel/health/resilience), `AddCoreWeb`/`UseCoreWeb`, JWT auth (`AddCoreAuthentication`),
  permission authz (`Authorization/`), `ValidationFilter`, OpenAPI/Scalar, `SecurityHeadersMiddleware`.

## Conventions here

- Public surface is **extension methods** that read as a two-call story: `builder.AddXxx()` then
  `app.UseXxx()`/`app.MapXxx()`. Keep `Program.cs` in consumers tiny.
- **`IExceptionHandler` order matters** — they run in registration order. `ValidationExceptionHandler`
  is registered before `GlobalExceptionHandler` (validation wins). Preserve that order in `AddCoreWeb`.
- Caching goes through `ICacheService` (wraps HybridCache). Don't take a hard dependency on `HybridCache`
  in feature code.
- Permission authorization: the dynamic `PermissionPolicyProvider` maps any `perm:<name>` policy; expose
  ergonomics via `RequirePermission(...)` / `[HasPermission]` rather than hand-rolled `[Authorize(Roles=)]`.
- These projects are `IsPackable=true`; avoid leaking app-specific assumptions (connection strings,
  concrete modules) into them.
- **They are NuGet packages, so their public surface is tracked:**
  - Every public member has an XML doc comment. The docs also appear in consumers' OpenAPI documents (R-041), so
    describe behaviour and keep the claims true to the code.
  - `PublicAPI.Unshipped.txt` must list every public API (RS0016/RS0017).
  - No two public overloads with optional parameters (RS0026). `ToHttpResult` has four plain overloads for this reason.
  - `Result<T>`'s own `Success`/`Failure` are internal (CA1000). Callers use `Result.Success<T>(…)`/`Result.Failure<T>(…)`
    or the implicit conversions.
