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
  Catalog.Contracts; Core.Abstractions has no ASP.NET/EF reference). **Keep these green** — they are the
  guardrails for the whole design.

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

`xUnit1051` (CancellationToken) is `NoWarn`'d for these simple `HttpClient` tests.
