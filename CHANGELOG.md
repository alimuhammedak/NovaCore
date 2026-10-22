# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Security

- The repository is public. CI pins every GitHub Action to a full commit SHA, grants each job only the permissions it
  needs, keeps the checkout token off disk, and lints the workflows with actionlint and zizmor. It scans the whole git
  history for secrets (Betterleaks), runs CodeQL on the C# code and OpenSSF Scorecard on the repository, and checks
  that every package in the dependency graph is MIT or Apache-2.0 licensed.
- Releases build in a read-only job. A separate `publish` job in the `release` environment, which needs the owner's
  approval, signs SLSA build provenance for every package and pushes it. Each library package gets a CycloneDX SBOM.
- Restores use committed lock files in CI (`--locked-mode`), and the SDK is pinned exactly (`global.json`).
  Dependabot proposes NuGet, action, SDK and compose image updates after a 7-day cooldown.
- The compose services listen on 127.0.0.1 only and their images are pinned by digest.
- Identity provider: an authenticator code signs in (or enables or disables two-factor authentication) once. The code
  that enabled two-factor authentication no longer signs in, and two requests racing with one code let one in.
- Identity provider: two-factor recovery codes are stored as password hashes (PBKDF2), no longer as plain text.
  **Upgrade:** codes issued by an earlier version stop working; users get new ones from `POST /api/v1/auth/2fa/enable`.
  Delete the old plain-text codes with
  `DELETE FROM identity.AspNetUserTokens WHERE LoginProvider = '[AspNetUserStore]' AND Name = 'RecoveryCodes'`.
- Identity provider: the two-factor endpoints ask for the current password (ASVS 7.5.1). **Breaking:**
  `POST /api/v1/auth/2fa/setup` takes `{ password }`, `/2fa/enable` and `/2fa/disable` take `{ password, code }`. A wrong
  password or code counts toward lockout. `/2fa/enable` while two-factor authentication is on returns new recovery
  codes. Turning it on or off ends every session of the user (refresh tokens and authorizations).
- Identity provider: granting or removing the `Administrator` role needs `identity:roles:manage` on top of
  `identity:users:manage` (whose holder could otherwise make themselves an administrator), and the last administrator
  keeps the role (409). A role change through the admin API takes effect on the user's next request and revokes their
  refresh tokens and authorizations.
- Identity provider: the `Administrator` role counts only for an account with two-factor authentication on. **Upgrade:**
  administrators sign in with their password and turn it on (`/api/v1/auth/2fa/setup`, then `/2fa/enable`) before the
  admin API answers them again.
- Identity provider: the User role's permissions come from `Identity:Seed:UserPermissions` instead of "every declared
  `:read` permission". The host doesn't start when the list names a permission no module declares or one of the
  provider's own (`identity:*`). **Upgrade:** list the permissions every registered account should get; seeding still
  adds missing ones and never removes one.
- Payments: a payment belongs to the caller who made it (`OwnerId`, the token's `sub`; migration `PaymentOwner`).
  `GET /api/v1/payments` lists the caller's own payments; the new `payments:read-all` permission lists everyone's.
  **Upgrade:** payments made before have no owner and are listed with `payments:read-all` only.
- Identity provider: the API accepts only `at+jwt` tokens from it (RFC 9068 §4), so no other JWT signed with its key
  passes for an access token. The core alone still accepts other providers' `JWT` tokens.

### Added

- `HybridCache.RemovePermissionsAsync(subject)` (`NovaCore.AspNetCore`): drops the permissions cached for a subject, so
  a change takes effect on its next request instead of after `Cache:PermissionExpiration`.

### Changed

- Template: the generated CI workflow pins its actions to commit SHAs, narrows its permissions and doesn't persist the
  checkout token. A `.github/dependabot.yml` keeps the pins up to date.

## [0.1.0-preview.1] - 2026-10-20

The first published version (GitHub Packages, `BMD.NovaCore.*`). Everything below is relative to the code the
repository started from.

### Added

- `BMD.NovaCore.Templates`, a template package (`src/Templates/NovaCore.Templates`): `dotnet new novacore` creates a
  solution that references the NovaCore packages of the template's own version instead of copying this repository. It
  has an API host, a sample `Todos` module (CRUD-style slices, permissions, validation, `Result` → ProblemDetails, its
  own schema, migrations and outbox), integration tests with an in-memory OpenID Connect issuer, a `nuget.config` that
  maps the NovaCore packages to their feed, `appsettings.json` with a production checklist, and a CI workflow that checks
  the migrations against the model and, on the default branch, builds a migration bundle per DbContext. `--auth BuiltIn`
  (default) runs the identity provider in the host; `--auth External` only validates an external provider's tokens.
  Outside Development the host can answer probes on a management port (`HealthChecks:ProbePort`).
- `eng/template-smoke.sh` packs every package into a local feed, creates a project from the template with each identity
  option, and builds, tests and checks its migrations the way a consumer would. CI runs it as the `template-smoke` job.
- `NovaCore.Identity.OpenIddict` prunes its store: at startup and every hour, OpenIddict's `PruneAsync` deletes the tokens
  that are expired, revoked or redeemed and the authorizations that are no longer valid and have no token left, once
  they are 14 days old (OpenIddict.Quartz's defaults; no new package). A failed pass is logged as an error and retried on
  the next one.
- Password reset in `NovaCore.Identity.OpenIddict`: the sign-in page links to `/account/forgot-password`, which emails a
  confirmed account a link to `/account/reset-password` (from `Jwt:Authority`, never the Host header). The new password
  is typed twice; the link works once and expires with the other Identity tokens (6 hours). A reset revokes every refresh
  token and authorization of the user and doesn't sign them in. Unknown and unconfirmed addresses get the same page and
  no email. Audited as `SECURITY password_reset_requested` and `password_reset`.
- The sign-in page's two-factor step also takes one of the recovery codes `/api/v1/auth/2fa/enable` returns, once each
  (until now they couldn't be used). A locked account can't sign in with one, and a wrong one counts toward lockout like
  a wrong authenticator code. A used code is audited as `SECURITY recovery_code_used` (Warning).
- `POST /api/v1/auth/resend-confirmation-email` (anonymous, `strict` rate limit) sends the confirmation link again to an
  account that isn't confirmed yet; every address gets the same answer.

- Reverse proxy support: `UseCoreWeb` applies `X-Forwarded-For` and `X-Forwarded-Proto` first, from the proxies and
  networks listed under `ForwardedHeaders:KnownProxies` and `ForwardedHeaders:KnownIPNetworks` only (validated on
  start). Behind a trusted proxy the rate limiter partitions by the real client and the request scheme is the client's,
  so cookies get their `Secure` flag. With no list, no caller's headers apply (the framework default).

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
  (Abstractions ← AspNetCore / EntityFrameworkCore, add-ons → AspNetCore), and nothing under `src/` scans assemblies
  to register services. Host wiring tests pin the module order and endpoints, the validation response of every
  validated request, permission-based 200/403 and the Scalar UI.
- `Directory.Build.targets` fails the build of a packable project that does not reference PublicApiAnalyzers.
- New add-on package `NovaCore.Identity.OpenIddict`: an OpenID Connect provider for the host, built on ASP.NET Core
  Identity and OpenIddict (Apache-2.0). `builder.AddCoreIdentityServer()` registers it. Its issuer is `Jwt:Authority`,
  so the core validates its signed JWT access tokens through discovery and the published keys like any other
  provider's. PKCE is required (S256 only). Access tokens live 15 minutes, refresh tokens 7 days. Keys are ephemeral in
  Development; elsewhere the host adds certificates with OpenIddict's API, and the host does not start without them.
  The host registers `IdentityServerDbContext` (`AddModuleDbContext`) and keeps its migrations.
  `app.MapCoreIdentityServer()` maps `/connect/authorize`, `/connect/token`, `/connect/revoke`, `/connect/endsession`
  and a plain sign-in page at `/account/login` (antiforgery-protected, local return URLs only). `max_age` and the
  `auth_time` claim count from the sign-in, not from the session cookie's last renewal. Clients come from
  `Identity:Clients` and are synced into the store when `Identity:Seed:SeedOnStartup` is true. Refresh tokens rotate;
  reusing a redeemed one after OpenIddict's 30-second leeway revokes every token of that authorization, and a refresh
  is refused once the user is gone, can't sign in or is locked out.
  Service clients (configured without redirect URIs, with a secret) get tokens with client credentials; their `sub`
  is the client id and they hold the `Permissions` configured for them.
  The Identity module's hardening carries over: PBKDF2 with 600,000 iterations, a length-first password policy (12
  characters, no composition rules), unique emails, lockout after five failures for 15 minutes, a confirmed email before
  sign-in, and one sign-in error for unknown accounts, wrong passwords and locked accounts. Sign-ins, lockouts, refreshes
  and sign-outs go to the log as `SECURITY` events with masked emails.
  Self-service registration and email confirmation (`/api/v1/auth/register`, `/api/v1/auth/confirm-email`) carry over
  too. The confirmation link now starts from `Jwt:Authority` instead of the request's Host header. Outside Development
  the host must register an `IEmailSender`; in Development emails go to the log.
  Authenticator-app two-factor authentication carries over: `/api/v1/auth/2fa/setup|enable|disable` for the signed-in
  caller, and a code step on the sign-in page. A wrong code now counts toward lockout like a wrong password. The
  authenticator app shows the host's application name.
  The session endpoints carry over too: `/api/v1/auth/me`, `/api/v1/auth/change-password` and
  `/api/v1/auth/logout-all`. The last two revoke every refresh token and authorization of the user and end the sign-in
  session at the provider immediately (the cookie's security stamp is checked on every use); issued access tokens stay
  valid until they expire. Signing out of a single session is the standard `/connect/revoke` and `/connect/endsession`.
  The admin API (`/api/v1/admin/users`, `/roles`, `/permissions`, role assignment) carries over with its `identity:*`
  permissions. When `Identity:Seed:SeedOnStartup` is true the store is seeded: the Administrator role with every
  declared permission, the User role with the business read permissions, the administrator from
  `Identity:Seed:AdminEmail`/`AdminPassword`, then the clients. A new registration gets the User role.
- **Breaking:** the Api host runs `NovaCore.Identity.OpenIddict` as its own authority and the `NovaCore.Modules.Identity`
  module is removed. `POST /api/v1/auth/login`, `/login/2fa`, `/refresh` and `/logout` are gone: clients sign in with
  Authorization Code + PKCE at `/connect/authorize` and `/connect/token`, and sign out with `/connect/revoke` and
  `/connect/endsession`. The other `/api/v1/auth/*` and `/api/v1/admin/*` endpoints keep their paths. The identity
  store's migrations now live in the Api project (`Identity/Migrations`, context `IdentityServerDbContext`, schema
  `identity`); an `identity` schema created by the old module has to be dropped before migrating. In Development
  `Jwt:Authority` is the host's https address (`https://localhost:5443`, the https launch profile).
- **Breaking (configuration and API):** `Jwt:Authority` is required in every environment, and only asymmetric
  signatures are accepted: `Jwt:SigningKey`, `Jwt:Issuer` and the built-in Development key are removed, as are
  `Jwt:AccessTokenMinutes` and `Jwt:RefreshTokenDays` (the identity provider's lifetimes are set on
  `OpenIddictServerOptions`). `ITokenService`, `JwtTokenService` and `AccessToken` are removed from `NovaCore.AspNetCore`:
  nothing in NovaCore issues tokens locally any more.
- `docker-compose.yml` pins its images (SQL Server `2022-CU27-ubuntu-22.04`, Seq `2026.1.17182`, Jaeger v2 `2.21.0`
  instead of the end-of-life v1 all-in-one). New `docker-compose.keycloak.yml` adds an optional Keycloak `26.7.4` with an
  example `novacore` realm (`deploy/keycloak/novacore-realm.json`: a public PKCE client for Scalar, the `novacore.api`
  audience, realm roles as a `role` claim) on `http://localhost:8180`. `.env.example` lists the compose passwords.
- The OpenAPI document declares the identity provider's Authorization Code flow (`OAuth2` scheme, under
  `Jwt:Authority`), and `NovaCore.Scalar` has a `MapCoreScalar(Action<ScalarOptions>)` overload. The Api host uses it
  to sign Scalar in with PKCE as the `scalar` client (`Identity:Clients` in `appsettings.Development.json`).
- Transactional outbox, write side: the domain events an entity raises are written to its module's `outbox_messages`
  table by the SaveChanges that saves the entity, in the same transaction, and the entity's buffer is cleared.
  `modelBuilder.AddOutbox()` (`NovaCore.EntityFrameworkCore.Messaging`) maps the table in the context's schema, and
  `AddModuleDbContext` adds the writing `OutboxInterceptor` to every module context. A context whose entities raise
  events without an outbox in its model fails the save instead of dropping them. Catalog and Payments map the outbox
  (migration `Outbox`: `catalog.outbox_messages`, `payments.outbox_messages`); neither raises events yet.
- Transactional outbox, dispatch side: `AddCorePersistence` registers a background service that delivers the events in
  every module's outbox to their `IDomainEventHandler<T>`s. It runs a pass at startup, then every 5 seconds on
  `TimeProvider`, oldest message first. A failed message is retried after newer ones, so there is no ordering
  guarantee. Each message gets a DI scope, which its handlers share. Each handler that
  finishes is recorded in `outbox_consumers`, so a retried message doesn't run those handlers again. A failed delivery
  is retried on the next pass; after 10 attempts the message stays in the outbox with its last error, for inspection.
  No transaction of its own: it works under `EnableRetryOnFailure`. Delivery is at least once and handlers must be
  idempotent. One dispatcher per database is assumed. Migration `OutboxDispatch` adds `Attempts`, `Error` and
  `ProcessedOnUtc` to `outbox_messages`, and the `outbox_consumers` table.
- Stock reservation (sample modules, D-034): a payment now takes its product's stock, so a product can no longer be sold
  without limit. The two modules keep the stock and the payment consistent through the outbox:
  - A payment is saved pending together with its `PaymentCreated` event. The event lives in the new
    `NovaCore.Modules.Payments.Contracts` project, which other modules reference instead of the Payments module.
  - The Catalog module's `ReserveStockForPayment` takes the stock with `Product.RemoveStock`, or refuses when the
    product is gone, inactive or short of stock. Stock never goes below zero. Its answer is a `StockReservation` row
    keyed by the payment, saved with the stock change and with a `StockReserved` or `StockReservationFailed` event
    (`NovaCore.Modules.Catalog.Contracts`). A redelivered request finds the row and changes nothing.
  - The Payments module's answer handler completes or fails the payment. A payment settles once; the same answer again
    changes nothing, and the other answer is refused.
  - `POST /api/v1/payments` still answers 201 with a pending payment and keeps its 404/400/409 checks. The outcome shows
    up in `GET /api/v1/payments` within two dispatcher passes when the deliveries succeed; each failed delivery adds a
    pass. Two requests for the last unit can both be accepted; one of them then fails. Migration `StockReservations`
    adds `catalog.stock_reservations`.

### Changed

- Package IDs carry the `BMD.` prefix (`BMD.NovaCore.AspNetCore`, ...): other owners publish `NovaCore.*` packages on
  nuget.org, `NovaCore.AspNetCore` and `NovaCore.Templates` among them. Assembly and namespace names don't change.
- `dotnet new install .` no longer turns this repository into a template (the root `.template.config` is gone); the
  template is the `BMD.NovaCore.Templates` package.

- `AddCoreObservability` (`NovaCore.OpenTelemetry`) sets up the log signal itself: `ILogger` records go through
  OpenTelemetry with their rendered message and scopes, and are exported over OTLP with the metrics and traces when
  `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Before, OTLP export turned logs on implicitly without the message and scopes. A
  host that uses Serilog (`AddCoreLogging`) is unaffected: Serilog doesn't write to other logging providers.

- `outbox_consumers` is keyed by the message row (`MessageId`, the outbox's `bigint` key) and the handler, with a
  cascading foreign key, instead of the event id: narrow and ever-increasing like the outbox's own key. The
  `OutboxConsumersByMessage` migrations carry existing records over. The outbox dispatcher deletes processed messages,
  and with them their records, 7 days after they were processed; messages left for inspection stay.

- `ToHttpResult` returns typed unions instead of `IResult`: `Result` → `Results<NoContent, ProblemHttpResult>`,
  `Result<T>` → `Results<Ok<T>, ProblemHttpResult>`, and `ToHttpResult(value => TypedResults.Created(...))` →
  `Results<TSuccess, ProblemHttpResult>`; `Problem(Error)` returns `ProblemHttpResult`. The `(Result, int)` overload is
  removed. Module handlers return these unions and drop their success `.Produces<T>()`; the OpenAPI document is
  unchanged for them. The identity provider's `/api/v1/auth/*` and `/api/v1/admin/*` endpoints return typed results
  too, so their responses (bodies, 204s, 400/404/409) now appear in the OpenAPI document instead of an untyped 200.
  Response bodies are unchanged.

- `ArgumentException`, `FormatException`, `KeyNotFoundException`, `UnauthorizedAccessException` and an
  `OperationCanceledException` the server raised itself are answered with 500 instead of 400/404/403/499: they are bugs,
  not client errors. A client's abort is still the framework's 499. The framework's exception handler middleware logs
  the 500s (`UnhandledException`, Error) instead of the core; `AppException`s and bad requests are no longer logged, like
  a failed `Result`. Response titles and types are unchanged.

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
    `AppException` types, `ResultHttpExtensions`) → `NovaCore.AspNetCore`. The
    `NovaCore.Core.Infrastructure` package no longer exists.
  - New add-on `NovaCore.Serilog`: `AddCoreLogging` moved here from `AddCoreDefaults`, which no longer configures
    logging. `UseCoreWeb` no longer calls `UseSerilogRequestLogging`; the host calls it before `UseCoreWeb`.
  - New add-on `NovaCore.OpenTelemetry`: `AddCoreObservability` moved here from `AddCoreDefaults`, which now only adds
    health checks and the HttpClient resilience handler.
  - New add-on `NovaCore.Scalar`: `app.MapCoreScalar()` maps the Scalar UI in Development. `UseCoreWeb` still maps the
    OpenAPI document but no longer the Scalar UI.
  - `ValidationFilter<T>` and `ValidationExceptionHandler` (FluentValidation) are removed (see the validation entry
    below), so the core packages depend on Microsoft packages only.
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
  - an empty `Jwt:Issuer`/`Jwt:Audience`, non-positive token lifetimes, or `Jwt:RefreshTokenDays` above 36,500;
  - a non-positive `RateLimiting:PermitLimit`/`WindowSeconds` or `Cache:DefaultExpiration`;
  - a `Cors:AllowedOrigins` entry that is not a bare origin (`*`, or one with a path or trailing slash);
  - a `Database:ApplyMigrations` value that is not a boolean.
- **Breaking:** `JwtOptions` is registered as `IOptions<JwtOptions>` instead of a plain singleton, and
  `JwtTokenService` takes `IOptions<JwtOptions>`. `AddCoreAuthentication`, `AddCoreInfrastructure` and `AddCoreCaching`
  no longer take `IConfiguration` (nor `IHostEnvironment`); they read both from DI.
- The time comes from an injected `TimeProvider` (`TimeProvider.System` unless the host registers another):
  audit stamps, access-token `iat`/`exp`, refresh-token creation, expiry and revocation, user creation and the admin
  lockout listing. Entities no longer default `CreatedAtUtc` to the current time; it is `required`, and the code that
  creates them sets it.
- **Breaking:** `AccessToken` carries `IssuedAtUtc`, and `ExpiresInSeconds` is the token's lifetime
  (`ExpiresAtUtc - IssuedAtUtc`) instead of the time left when read. The token response's `expiresIn` is now the full
  lifetime (e.g. 900 instead of 899). `JwtTokenService` and `AuditableEntityInterceptor` take a `TimeProvider`.
- **Breaking:** `/health` and `/alive` are mapped in Development only (`MapDefaultEndpoints`). A production host that
  needs probes maps its own health endpoints.
- Every module DbContext registered with `AddModuleDbContext` gets a database health check (`AddDbContextCheck`, named
  after the context, tagged `ready`), so `/health` reports `Unhealthy` when a module database can't be reached.
  `NovaCore.EntityFrameworkCore` references `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`.
- **Breaking:** `DatabaseMigrationService` is internal. `AddCorePersistence` always registers it and it reads
  `Database:ApplyMigrations` when the host starts. `NovaCore.EntityFrameworkCore` references
  `Microsoft.Extensions.Options.ConfigurationExtensions` instead of `Microsoft.Extensions.Configuration.Binder` (which
  still comes transitively).
- **Breaking:** request validation is .NET 10's built-in Minimal API validation instead of FluentValidation.
  - The rules are DataAnnotations on the request records. Each module calls `services.AddValidation()` in
    `AddModule`, because the source generator only covers the assembly that makes the call.
  - The host no longer registers a validation exception handler, and there is no `AddValidationExceptionHandler()` to
    call.
  - The 400 response keeps its shape: `application/problem+json`, `type`, `status`, `instance`, `traceId`, and `errors`
    keyed by property name.
  - Its `title` is now the framework default "One or more validation errors occurred." instead of "Validation failed".
    The handlers' own validation responses already used that title. The messages are the DataAnnotations ones.
- **Breaking:** permission authorization no longer uses a custom policy provider.
  - `RequirePermissionAttribute` (an `AuthorizeAttribute` that implements `IAuthorizationRequirementData`) carries the
    `PermissionRequirement` itself.
  - `RequirePermission(...)` keeps its signature and now attaches that attribute.
  - `PermissionPolicyProvider`, with its `perm:` policy names, is removed, and so is `HasPermissionAttribute`. Use
    `[RequirePermission(...)]` instead.
  - Behaviour is unchanged: anonymous callers get 401, callers without the permission (or the `Administrator` role)
    get 403. .NET 10 enforces the attribute on Minimal API and routed endpoints.
- **Breaking (configuration):** JWT bearer tokens are validated against an OpenID Connect provider, `Jwt:Authority`.
  Its issuer and signing keys are discovered from `{Authority}/.well-known/openid-configuration` and its JWKS, and only
  asymmetric algorithms (RS/PS/ES 256–512) are accepted.
  - Outside Development `Jwt:Authority` is required and must be an absolute `https` URI.
  - The symmetric `Jwt:SigningKey` (HS256) is accepted in Development only; anywhere else the host refuses to start
    with one. In Development, with neither setting, the built-in development key still applies.
  - As a result the Identity module's own HS256 tokens are validated only in Development, and issuing one elsewhere
    fails with a message naming `Jwt:SigningKey` (`JwtTokenService`).
- **Breaking (behaviour):** permissions are resolved on the server, never read from the access token.
  `AddCoreAuthorization` registers a claims transformation that discards any `permission` claim in the token and adds
  the permissions a registered `IPermissionResolver` returns. They are cached in HybridCache per `sub` and security
  stamp (`NovaClaims.SecurityStamp`) for `Cache:PermissionExpiration` (default 5 minutes, validated on start), so a role
  change reaches a signed-in user within that time. A host must register an `IPermissionResolver`, or it refuses to
  start in any environment; the Identity module
  registers `IdentityPermissionResolver` (role and direct permission claims from its store), and its access tokens no
  longer carry `permission` claims.
- **Breaking (API):** the domain-event buffer (`DomainEvents`, `Raise`, `ClearDomainEvents`) moved from `Entity<TId>` to
  a new non-generic base class, `Entity`, so the persistence layer can read any entity's events without its key type.
  Code that derives from `Entity<TId>` or `AuditableEntity<TId>` compiles unchanged.
- `PUT` and `DELETE /api/v1/products/{id}` (Catalog sample module) answer 409 with code `product.concurrency_conflict`
  when the product's stock changed after the request read it. `Stock` is now a concurrency token, so a write can't
  undo a sale. The OpenAPI document lists the 409 response for both. Migration `StockConcurrencyToken` only brings the
  model snapshot up to date; the schema doesn't change.
- **Breaking (API and behaviour):** `IDomainEventDispatcher` is registered scoped and resolves the handlers from the
  caller's scope; as a singleton it resolved them from the root provider, which failed for scoped handlers. Each event
  type's handlers are invoked through a cached, strongly typed wrapper instead of `MethodInfo.Invoke` per call.
  `DomainEventDispatcher` is now internal; use `IDomainEventDispatcher`.

### Removed

- `IDomainEventDispatcher` (and its implementation), the in-process way to run domain-event handlers. Nothing called
  it, and an event dispatched through it skipped the transactional outbox. Events go through the outbox only.

- `GlobalExceptionHandler`. Its mapping is now the exception handler middleware's own fallback
  (`ExceptionHandlerOptions.StatusCodeSelector` and the ProblemDetails customization), so an `IExceptionHandler` a project
  registers runs first, whether it was added before or after `AddCoreWeb`; before, the catch-all handler registered by
  `AddCoreWeb` answered first and later handlers never ran.

- Unused dependencies and wiring: `Scrutor` (never called), `Asp.Versioning.Http` + `AddApiVersioning` (no endpoint used a
  version set), `AddOutputCache`/`UseOutputCache` (no policy or endpoint used it).
- **Breaking:** `PagedList<T>.Empty(int)` (it had no callers).
- **Breaking:** `ICacheService` and `CacheEntryOptions` (`NovaCore.Abstractions`) and `HybridCacheService`
  (`NovaCore.AspNetCore`). .NET's `HybridCache` is already an abstraction, so inject it directly. `AddCoreCaching` still
  registers it with `Cache:DefaultExpiration` as the default entry lifetime.
- The `EXTEXP0018` warning suppression. `Microsoft.Extensions.Caching.Hybrid` 10.0.0 no longer marks its APIs
  experimental.
- The `FluentValidation` and `FluentValidation.DependencyInjectionExtensions` packages: they are gone from central
  package management and from the package-policy allowlist, since nothing uses them after the move to built-in
  validation.
- `Payment.Refund()` and `PaymentStatus.Refunded` (Payments sample module): nothing called them.

### Security

- `/connect/endsession` no longer signs a browser out on any GET: a signed-in browser gets a question, and only its form's
  POST, with an antiforgery token, ends the session (logout CSRF). A signed-out browser goes on to the post-logout
  redirect as before.
- The sign-in page checks a password hash for an unknown email too, so its response time no longer reveals which
  addresses have accounts.
- A wrong current password at `/api/v1/auth/change-password` counts toward lockout (audited as `SECURITY
  account_locked_out` when it locks the account), and a locked account's password doesn't change; before, a stolen access token allowed unlimited guesses within the rate limit.

- `System.Security.Cryptography.Pkcs` 9.0.11, which Microsoft.Data.SqlClient brings under EF Core's SQL Server
  provider, had two high-severity denial-of-service advisories (CVE-2026-84008, CVE-2026-70162; GHSA-846f-f764-76mx,
  GHSA-2fmv-rv2c-59x7) and failed the restore. Every project that references the SQL Server provider (the Catalog and
  Payments modules, the Api host) references the patched 10.0.13 directly; no packable package is affected.

- `NovaCore.Identity.OpenIddict` stops the host outside Development unless the host configured a Data Protection key
  store (`PersistKeysToFileSystem`, `PersistKeysToDbContext`, a blob or Redis store). The sign-in cookies, antiforgery
  tokens and email confirmation links are protected with these keys; the framework's fallback store is lost when a
  container is replaced and differs between instances, which signed users out and broke links without an error.

- The `Administrator` role no longer bypasses permission checks through the token's `role` claim, which a removed
  administrator kept until the token expired. Permissions, an administrator's included, are resolved on the server:
  the identity provider's resolver gives an `Administrator` member every permission the modules declare, so a removed
  administrator loses them within `Cache:PermissionExpiration`. `HttpContextCurrentUser.SuperAdminRole` is removed and
  `ICurrentUser.HasPermission` checks the resolved permissions only.

- Authorization is closed by default: `AddCoreAuthorization` sets a fallback policy that requires an authenticated
  caller, so an endpoint without authorization metadata (a forgotten `RequirePermission`) is no longer public, and a
  request that matches no endpoint gets 401 instead of 404 when anonymous. The endpoints meant to be public declare
  `AllowAnonymous`: health, the OpenAPI document, Scalar, the identity provider's `/connect/*` endpoints and sign-in
  pages, registration and email confirmation.
- With an empty `Cors:AllowedOrigins`, only Development allows any origin; other environments allow none (previously
  every origin, in every environment).

- `docker-compose.yml` reads the SQL Server SA password from a git-ignored `.env` file instead of a hard-coded value.
- Carried-over account flaws closed in `NovaCore.Identity.OpenIddict`: the sign-in page says "confirm your email" only
  after the right password, and a wrong one counts toward lockout (the old login answered "email not confirmed" for any
  password, which revealed unconfirmed accounts); `/api/v1/auth/2fa/setup` answers 409 once two-factor authentication
  is enabled instead of returning the authenticator key, with which the holder of an access token could compute codes
  and turn 2FA off.
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

- Registering through `NovaCore.Identity.OpenIddict` without a seeded `User` role (seeding off, the Development
  default) answered 500 after creating the account, and sent no confirmation email. The account now starts without a
  role and gets its confirmation link.

- A request body Minimal APIs can't read (malformed JSON in Development, where they throw `BadHttpRequestException`)
  answers with the exception's status (400) instead of 500.

- The ProblemDetails `instance` member is a URI reference, the request path (e.g. `/api/v1/products/42`), instead of
  `"GET /api/v1/products/42"`. The query string is left out.

- The `"strict"` rate-limit policy (used by `/api/v1/auth`) is now partitioned per client IP. Before, it was a single
  shared counter, so one client could exhaust it and lock every other client out of login.
- `IDomainEvent.EventId` and `OccurredOnUtc` returned a new value on every read (default interface members). They are
  now plain interface properties, and the new `DomainEvent` base record fixes both when the event is created.
  **Breaking:** events must derive from `DomainEvent` (or implement the two properties themselves).
