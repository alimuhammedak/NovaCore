# CLAUDE.md — Tests

Subtree rules for `tests/`. See the repo-root `CLAUDE.md` for global standards.

Stack: **xUnit v3** (test projects are `OutputType=Exe`). Run a single test with
`dotnet test --filter "FullyQualifiedName~ClassName.MethodName"`.

## Three flavors, one project

- **Unit** (`Unit/`) — pure logic (`Result<T>`, pagination). No host.
- **Integration** (`Integration/`) — boot the real host via `NovaCoreApiFactory` (`WebApplicationFactory`).
  DB-free tests (health, OpenAPI, security headers, 401/validation gates) need no seeding. A caller with specific roles
  comes from `factory.CreateClientWithClaims(...)`: the factory's in-memory issuer (`factory.IdentityProvider`, see
  below) signs the token, so no user is seeded (`ValidationTests`). Permissions are never read from the token, so a
  caller with permissions comes from `factory.CreateClientWithPermissions(...)` (`PermissionAuthorizationTests`). DB-backed tests (`AuthFlowTests`) implement `IAsyncLifetime` and seed via
  `IdentityDataSeeder` + `UserManager`; they use the Identity module's own HS256 tokens.
- **Architecture** (`Architecture/`) — `ModularityTests` enforce boundaries (Payments → only
  Catalog.Contracts; `NovaCore.Abstractions` has no ASP.NET/EF reference). The testable bans in
  `docs/plan/DECISIONS.md` are tests too: banned packages anywhere in the dependency graph (Y-01/32/33),
  core packages on Microsoft packages only plus the one-way package direction (Y-05, `PackageDependencyTests`, which
  reads the `src/BuildingBlocks` csproj files and the built assemblies), module references only via `*.Contracts`
  and no extra per-module projects (Y-08/17), no `IRepository`/`IUnitOfWork` or `*Helper`/`*Util`/`*Manager` types
  (Y-15/29), no assembly scanning or `*FromAssembly*` service registration anywhere under `src/` (Y-20; EF's
  `ApplyConfigurationsFromAssembly` is allowed, D-023), scope + build validation on the test host (Y-26),
  no `*.Domain` types in endpoint signatures via ApiExplorer (Y-31), no TODO/FIXME under `src/` (Y-38). `Solution`
  locates the repo root and first-party assemblies.
- **Host wiring** (`Integration/`) — `ModuleRegistrationTests` pin the module order and every module endpoint;
  `ValidationTests` pins the 400 validation ProblemDetails (type, instance, traceId, error keys) of every validated
  request, so a module that forgets `services.AddValidation()` fails here. Update the expected lists when you add or
  change an endpoint.
  **Keep these green** — they are the guardrails for the whole design; never weaken one to pass (Y-10).

## How the test host works (and why)

`NovaCoreApiFactory` swaps **every module DbContext** (Identity, Catalog, Payments) from SQL Server to its own
**in-memory SQLite** connection (`ReplaceWithSqlite<TContext>`), so the full auth flow and the `/health` database
checks run without a database. Two non-obvious requirements, both load-bearing:

1. **Strip every registration generic over the context before re-adding it.** EF Core 10 registers
   the provider via additive `IDbContextOptionsConfiguration<TContext>`, so removing only
   `DbContextOptions<TContext>` leaves the SQL Server config applied → "multiple providers".
2. **Give the SQLite contexts their own internal service provider** (`new ServiceCollection()
   .AddEntityFrameworkSqlite().BuildServiceProvider()` + `.UseInternalServiceProvider(...)`). The app still
   registers SQL Server, and EF forbids two providers in one container.

The SQLite connections are opened in the factory ctor and disposed in `Dispose` to keep the in-memory databases
alive for the test class. Only Identity's schema is created (`EnsureCreated` in the auth tests); the others just need
to be reachable. A subclass can call `ReplaceWithSqlite` again to point one context elsewhere, e.g. at a missing file
to make its health check fail (`HealthEndpointsTests`).

## Tokens: an in-memory OpenID Connect issuer

The core validates access tokens against `Jwt:Authority` (D-002, D-024). `TestIdentityProvider` is that authority for
tests: an in-memory RSA key, with the discovery document and JWKS served by an `HttpMessageHandler`. The factory sets
`Jwt:Authority` to `TestIdentityProvider.Issuer` in every environment and wires the handler as
`JwtBearerOptions.BackchannelHttpHandler` (in `Configure`: JwtBearer builds the backchannel in `PostConfigure`, R-056).
So tokens go through the real Authority → metadata → JWKS path with no network.

- `factory.IdentityProvider.CreateToken(claims)` signs with the published key.
- `TestIdentityProvider.CreateToken(claims, key, algorithm, issuer, audience, expires)` breaks one property at a time
  for rejection tests (`JwtValidationTests`).
- The symmetric `Jwt:SigningKey` is set in Development only (outside Development it stops the host), so
  `AuthFlowTests` and `RefreshTokenExpiryTests` can log in through the Identity module, which still signs HS256.
- **Permissions** are resolved on the server (`IPermissionResolver`). The factory wraps the host's resolver: a subject
  of `IdentityProvider`'s tokens holds what `factory.GrantPermissions(subject, ...)` gave it (nothing by default), and
  every other token (the Identity module's) goes to the host's resolver. `CreateClientWithPermissions(...)` does both
  for a fresh subject.
- Resolved permissions are cached (`Cache:PermissionExpiration`). `FakeClockApiFactory` drives the memory cache's
  clock too (`MemoryCacheOptions.Clock`: HybridCache's local entries expire by it, not by `TimeProvider`, R-057), so
  `PermissionResolutionTests` moves the clock past the lifetime instead of waiting. **Gotcha:** the Identity module
  stamps its tokens from that fake clock while JWT validation reads the real one (R-058), so never log in through
  Identity after a shared fixture's clock was advanced (the token would be "not yet valid", 401). That is why
  `RoleChangeTests` has its own host.

## The identity provider add-on: its own host

`IdentityServer/` tests `NovaCore.Identity.OpenIddict` on `IdentityServerTestHost`: a `WebApplication` composed only of
the core packages and the add-on (no modules, no Api project), on TestServer at `https://localhost` with an in-memory
SQLite store. That address is also `Jwt:Authority`, and JwtBearer's backchannel goes to the same server, so the add-on's
tokens are validated through its own discovery document and keys. `OidcFlow` plays the browser (cookies, no automatic
redirects) and the public client: authorize → sign-in form (antiforgery token) → code → token endpoint.
`IdentityServerFixture` shares one host per class; tests that need a different host start their own
(`IdentityServerTestHost.StartAsync(environment, configure)`). These classes run in the `IdentityServerTestGroup`
collection, apart from the parallel tests: their CPU load made the `WebApplicationFactory` startup race (R-055) hit
most full runs.

## Test configuration — in-memory config is enough

The building blocks bind configuration through the options pattern and read it when the options are first built,
not during registration (docs/plan/RESEARCH.md R-048). So `NovaCoreApiFactory`'s `ConfigureAppConfiguration` values
(the test authority, the Development signing key, migrations off, seeding off, a high rate limit) reach the app like
any other configuration. The high limit keeps the many `/auth` calls from tripping the `"strict"` policy (which would
return 429). Don't set environment variables in tests: they are process-wide and race the other test classes.

**One factory needs a different value?** Subclass `NovaCoreApiFactory`, override `ConfigureWebHost`, call `base` and
then add another `ConfigureAppConfiguration`: sources added later win. Examples: `StrictRateLimitApiFactory`, and the
factory in `ConfigurationValidationTests`. Invalid values stop the host on start (`ValidateOnStart`), so a test can
assert on `factory.Server` throwing.

**Different environment?** Override `EnvironmentName` (default `Development`), e.g. `ProductionApiFactory`.

`xUnit1051` (CancellationToken) is `NoWarn`'d for these simple `HttpClient` tests.

Warnings are errors here too. Test names are sentences with underscores (`Refresh_rotates_the_refresh_token`), so
`.editorconfig` turns CA1707 off for `tests/**.cs` only (DECISIONS D-011). Every other analyzer rule applies.
