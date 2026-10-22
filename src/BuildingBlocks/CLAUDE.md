# CLAUDE.md — BuildingBlocks (the reusable core)

Subtree rules for `src/BuildingBlocks/`. See the repo-root `CLAUDE.md` for global standards.

These are the NuGet packages a NovaCore host builds on (docs/plan/ANALYSIS.md §4), so keep them
**module-agnostic**: nothing here may reference `NovaCore.Modules.*`.

## Packages — put new code in the lowest package that fits

**Core** (BCL and Microsoft packages only, D-003; enforced by `PackageDependencyTests`):

- **`NovaCore.Abstractions`** — BCL only. **No `FrameworkReference`, no NuGet, no ASP.NET, no EF.**
  Home of `Result`/`Result<T>`/`Error`, `PagedList<T>`/`PaginationRequest`, `Entity<TId>`/`AuditableEntity<TId>`
  (their domain-event buffer lives on the non-generic `Entity` base, so persistence reads it without the key type),
  `IDomainEvent` (events derive from the `DomainEvent` record, which fixes `EventId`/`OccurredOnUtc` at
  creation), `ICurrentUser`. If a new type needs a framework, it does **not** belong here.

- **`NovaCore.AspNetCore`** — the web host: `IModule` + `AddModule<TModule>()`/`MapModules`, `AddCoreDefaults`
  (health + HttpClient resilience), `AddCoreWeb`/`UseCoreWeb`, the exception mapping (`ExceptionMapping`, RFC 9457) and
  the `AppException` types, `ResultHttpExtensions.ToHttpResult`, JWT auth (`AddCoreAuthentication`: `Jwt:Authority` +
  JWKS, asymmetric keys only, D-028), permission authz (`IPermissionResolver` + a claims transformation that
  resolves permissions on the server and caches them in HybridCache)
  (`Authorization/`), the OpenAPI document, `SecurityHeadersMiddleware`, HybridCache (`AddCoreCaching`). Depends on
  Abstractions + `FrameworkReference Microsoft.AspNetCore.App`, never on EntityFrameworkCore.

- **`NovaCore.EntityFrameworkCore`** — persistence without ASP.NET Core: `AddModuleDbContext` (also a "ready"-tagged
  `AddDbContextCheck` per context, and the `OutboxInterceptor` on every module context), `AddOutbox()` (maps a module's
  `outbox_messages` and `outbox_consumers`), `AddCorePersistence` (`AuditableEntityInterceptor`,
  `DatabaseMigrationService`, which migrates only when `Database:ApplyMigrations` is true, and the outbox dispatcher).
  The dispatcher resolves and invokes the handlers of an event type through a `DomainEventHandlerInvoker<TEvent>` built
  once per type, so dispatching costs no reflection. There is no in-process dispatcher: an event that skipped the
  outbox would skip its transaction too (F4c.7). Depends on Abstractions,
  EF Core and `Microsoft.Extensions` hosting/options/health-check packages only, never on AspNetCore (D-020).
  `OutboxInterceptor` adds the outbox rows in `SavingChanges`, which EF Core runs before it detects changes and saves,
  so they share the save's transaction and a retrying execution strategy replays them with the entities (R-068).
  `AddCorePersistence` also registers `OutboxDispatcher` (a `BackgroundService`): a pass at startup, then every 5 seconds
  on a `PeriodicTimer` driven by `TimeProvider`. It reads each outbox in `Id` order in keyset batches, gives each message
  a DI scope for its handlers and records the finished handlers in `outbox_consumers` (key: the message's `Id` + the
  handler, FK with cascade delete). Each pass then deletes the processed messages older than `ProcessedRetention`
  (7 days); the age is compared in memory over `Id`-ordered batches, since SQLite can't compare `DateTimeOffset` in SQL
  (R-068). It opens no transaction, so every query and write is a retriable unit of the context's execution strategy.
  The interval, batch size, 10 attempts and the retention are constants: no consumer needs them configurable yet (Y-19).

**Add-ons** (optional, one third-party library each; they may reference AspNetCore/Abstractions only):
`NovaCore.Serilog` (`AddCoreLogging`), `NovaCore.OpenTelemetry` (`AddCoreObservability`: `ILogger` logs with their
message and scopes, metrics and traces, all over OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set; a Serilog host keeps
its logs in Serilog, which doesn't write to other providers), `NovaCore.Scalar`
(`MapCoreScalar`), `NovaCore.Identity.OpenIddict` (`AddCoreIdentityServer`: an OpenID Connect provider on ASP.NET Core
Identity + OpenIddict, D-026). A new third-party integration becomes a new add-on, not a dependency of the core.

`NovaCore.Identity.OpenIddict` owns `IdentityServerDbContext` but no migrations: it can't reference
NovaCore.EntityFrameworkCore, so the host registers the context with `AddModuleDbContext` (provider + `MigrationsAssembly`)
and keeps the migrations (R-060). Its issuer is `Jwt:Authority` and its access tokens are unencrypted signed JWTs, so
the core's JwtBearer validates them through discovery like any provider's (R-059). Tests run it on
`tests/.../IdentityServer/IdentityServerTestHost`, a host composed only of the core and the add-on. Two traps (R-061):
the host pipeline has no antiforgery middleware, so form posts validate with `IAntiforgery.IsRequestValidAsync` in
the handler; and the core's claims transformation runs on every `AuthenticateAsync`, OpenIddict's scheme included, so
a principal copied into a token must drop the resolved `permission` claims.

Request validation is not a building block: .NET 10's built-in validation needs `services.AddValidation()` in the
assembly that declares the request types, so each module calls it (root `CLAUDE.md` Standards, R-052).

## Conventions here

- Public surface is **extension methods** that read as a two-call story: `builder.AddXxx()` then
  `app.UseXxx()`/`app.MapXxx()`. Keep `Program.cs` in consumers tiny.
- **Configuration is validated options, read lazily** (root `CLAUDE.md` Standards, R-048). New settings get an options
  type (internal unless a consumer needs it), `BindConfiguration` + `ValidateOnStart`, and an `[OptionsValidator]`.
- **Time comes from `TimeProvider`**: register `TimeProvider.System` with `TryAddSingleton` where its consumer is
  registered, so the package works whichever `AddCore*` the host calls first.
- **Health endpoints are Development-only** (`MapDefaultEndpoints`, D-022); `/health` includes every module database.
- **The core's exception mapping is no `IExceptionHandler`.** Those run in registration order and the first `true` wins
  (R-046), so a catch-all one would shadow a project's handler registered after it. The mapping is the exception
  handler middleware's fallback instead: `ExceptionHandlerOptions.StatusCodeSelector` (`AppException` → its type's
  status, `BadHttpRequestException` → its status, else 500), `ExceptionHandlerOptions.ExceptionHandler` (writes the
  ProblemDetails: title, `type`, and `detail` for an `AppException` only; it runs only when no handler handled the
  exception, so a project handler's ProblemDetails is never rewritten; not `CustomizeProblemDetails`, which sees every
  write) and `SuppressDiagnosticsCallback` (the framework logs 500s; client
  errors and handler-handled exceptions aren't logged). Don't add a core `IExceptionHandler` (R-079).
- Caching: inject `HybridCache` directly. It is already an abstract class with a default implementation (R-008), so
  don't wrap it (Y-15). `AddCoreCaching` registers it with the default entry lifetime from `Cache:DefaultExpiration`.
- **Forwarded headers are opt-in by proxy:** `UseCoreWeb` runs `UseForwardedHeaders` first; `ForwardedHeaders:KnownProxies`
  and `KnownIPNetworks` (validated on start) replace the framework's loopback defaults, and only `X-Forwarded-For` and
  `X-Forwarded-Proto` apply. Empty leaves `ForwardedHeadersOptions` untouched (nothing applied). Anything that reads the
  client IP or the scheme reads `HttpContext.Connection.RemoteIpAddress`/`Request.Scheme` after this middleware.
- **Authorization is closed by default:** `AddCoreAuthorization` sets `FallbackPolicy = RequireAuthenticatedUser`, so an
  endpoint without authorization metadata, and a request that matches none, needs a signed-in caller (R-073). An
  endpoint a building block maps for anonymous callers (health, the OpenAPI document, Scalar, the identity provider's
  `/connect/*` and `/account/*`) says `.AllowAnonymous()`; `SecureDefaultsTests` pins them. CORS with an empty
  `Cors:AllowedOrigins` allows any origin in Development only.
- Permission authorization: `RequirePermissionAttribute` carries a `PermissionRequirement`
  (`IAuthorizationRequirementData`, R-053), which `PermissionAuthorizationHandler` checks. There is no custom policy
  provider and no named policy per permission. Use `RequirePermission(...)` / `[RequirePermission]` rather than
  hand-rolled `[Authorize(Roles=)]`; `PermissionAuthorizationTests` pins 200/403.
- The permission cache's entries are tagged by subject (`PermissionCacheExtensions.SubjectTag`); code that changes what
  the resolver returns calls `HybridCache.RemovePermissionsAsync(sub)`, which reaches this server's memory and the
  distributed cache, not another server's memory (R-100). Keep the key's format and the tag's escaping together.
- Package IDs are `BMD.` + the project name (`Directory.Build.targets`, D-041); the project, assembly and namespace
  names don't change. A dependency between two packages is a `ProjectReference`; pack turns it into the `BMD.` ID.
- The packages are `IsPackable=true`; avoid leaking app-specific assumptions (connection strings,
  concrete modules) into them. A new package needs `GenerateDocumentationFile`, the PublicApiAnalyzers reference and
  the two `PublicAPI.*.txt` files; `Directory.Build.targets` fails the build without the first two.
- **They are NuGet packages, so their public surface is tracked:**
  - Every public member has an XML doc comment. The docs also appear in consumers' OpenAPI documents (R-041), so
    describe behaviour and keep the claims true to the code.
  - `PublicAPI.Unshipped.txt` must list every public API (RS0016/RS0017).
  - No two public overloads with optional parameters (RS0026). `ToHttpResult` has three plain overloads, each returning
    a `Results<TSuccess, ProblemHttpResult>` union (F4c.5).
  - `Result<T>`'s own `Success`/`Failure` are internal (CA1000). Callers use `Result.Success<T>(…)`/`Result.Failure<T>(…)`
    or the implicit conversions.
