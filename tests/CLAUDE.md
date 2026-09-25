# CLAUDE.md — Tests

Subtree rules for `tests/`. See the repo-root `CLAUDE.md` for global standards.

Stack: **xUnit v3** (test projects are `OutputType=Exe`). Run a single test with
`dotnet test --filter "FullyQualifiedName~ClassName.MethodName"`.

## Three flavors, one project

- **Unit** (`Unit/`) — pure logic (`Result<T>`, pagination). No host.
- **Integration** (`Integration/`) — boot the real host via `NovaCoreApiFactory` (`WebApplicationFactory`).
  DB-free tests (health, OpenAPI, security headers, 401/validation gates) need no seeding. DB-backed tests
  (`AuthFlowTests`) implement `IAsyncLifetime` and seed via `IdentityDataSeeder` + `UserManager`.
- **Architecture** (`Architecture/`) — `ModularityTests` enforce boundaries (Payments → only
  Catalog.Contracts; Core.Abstractions has no ASP.NET/EF reference). The testable bans in
  `docs/plan/DECISIONS.md` are tests too: banned packages anywhere in the dependency graph (Y-01/32/33),
  module references only via `*.Contracts` and no extra per-module projects (Y-08/17), no
  `IRepository`/`IUnitOfWork` or `*Helper`/`*Util`/`*Manager` types (Y-15/29), scope + build validation
  on the test host (Y-26), no `*.Domain` types in endpoint signatures via ApiExplorer (Y-31), no
  TODO/FIXME under `src/` (Y-38). `Solution` locates the repo root and first-party assemblies.
  **Keep these green** — they are the guardrails for the whole design; never weaken one to pass (Y-10).

## How the test host works (and why)

`NovaCoreApiFactory` swaps the **Identity** store from SQL Server to a **shared in-memory SQLite**
connection so the full auth flow runs without a database. Two non-obvious requirements, both load-bearing:

1. **Strip every registration generic over `IdentityDbContext` before re-adding it.** EF Core 10 registers
   the provider via additive `IDbContextOptionsConfiguration<TContext>`, so removing only
   `DbContextOptions<IdentityDbContext>` leaves the SQL Server config applied → "multiple providers".
2. **Give the SQLite context its own internal service provider** (`new ServiceCollection()
   .AddEntityFrameworkSqlite().BuildServiceProvider()` + `.UseInternalServiceProvider(...)`). The app still
   has SQL Server registered (Catalog/Payments), and EF forbids two providers in one container.

The single SQLite connection is opened in the factory ctor and disposed in `Dispose` to keep the
in-memory DB alive for the test class.

## Config gotcha — use environment variables, not in-memory config

`RateLimiting:PermitLimit` and `Jwt:SigningKey` are read **eagerly** while the host builds services, before
`WebApplicationFactory`'s `ConfigureAppConfiguration` is applied. Set them as **env vars in the factory
constructor** (`RateLimiting__PermitLimit`, `Jwt__SigningKey` — `__` is the section separator). Tests bump
the rate limit high so the many `/auth` calls don't trip the `"strict"` policy (which would return 429).

**One factory needs a different eager value?** Don't touch the env var (it is process-wide and races the
other test classes). Subclass `NovaCoreApiFactory`, override `CreateHost` and add the value with
`builder.ConfigureHostConfiguration(...)`: host configuration reaches `Program` as `args` and beats env vars
(docs/plan/RESEARCH.md R-026). Example: `StrictRateLimitApiFactory`.

**Different environment?** Override `EnvironmentName` (default `Development`), e.g. `ProductionApiFactory`.

`xUnit1051` (CancellationToken) is `NoWarn`'d for these simple `HttpClient` tests.

Warnings are errors here too. Test names are sentences with underscores (`Refresh_rotates_the_refresh_token`), so
`.editorconfig` turns CA1707 off for `tests/**.cs` only (DECISIONS D-011). Every other analyzer rule applies.
