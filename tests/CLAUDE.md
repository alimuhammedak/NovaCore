# CLAUDE.md — Tests

Subtree rules for `tests/`. See the repo-root `CLAUDE.md` for global standards.

Stack: **xUnit v3** (test projects are `OutputType=Exe`). Run a single test with
`dotnet test --filter "FullyQualifiedName~ClassName.MethodName"`.

## Three flavors, one project

- **Unit** (`Unit/`) — pure logic (`Result<T>`, pagination). No host.
- **Integration** (`Integration/`) — boot the real host via `NovaCoreApiFactory` (`WebApplicationFactory`).
  DB-free tests (health, OpenAPI, security headers, 401/validation gates) need no seeding. A caller with specific roles
  or permissions comes from `factory.CreateClientWithClaims(...)`: the host's own `ITokenService` signs the token, so no
  user is seeded (`ValidationTests`). DB-backed tests (`AuthFlowTests`) implement `IAsyncLifetime` and seed via
  `IdentityDataSeeder` + `UserManager`.
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

## Test configuration — in-memory config is enough

The building blocks bind configuration through the options pattern and read it when the options are first built,
not during registration (docs/plan/RESEARCH.md R-048). So `NovaCoreApiFactory`'s `ConfigureAppConfiguration` values
(signing key, migrations off, seeding off, a high rate limit) reach the app like any other configuration. The high
limit keeps the many `/auth` calls from tripping the `"strict"` policy (which would return 429). Don't set
environment variables in tests: they are process-wide and race the other test classes.

**One factory needs a different value?** Subclass `NovaCoreApiFactory`, override `ConfigureWebHost`, call `base` and
then add another `ConfigureAppConfiguration`: sources added later win. Examples: `StrictRateLimitApiFactory`, and the
factory in `ConfigurationValidationTests`. Invalid values stop the host on start (`ValidateOnStart`), so a test can
assert on `factory.Server` throwing.

**Different environment?** Override `EnvironmentName` (default `Development`), e.g. `ProductionApiFactory`.

`xUnit1051` (CancellationToken) is `NoWarn`'d for these simple `HttpClient` tests.

Warnings are errors here too. Test names are sentences with underscores (`Refresh_rotates_the_refresh_token`), so
`.editorconfig` turns CA1707 off for `tests/**.cs` only (DECISIONS D-011). Every other analyzer rule applies.
