# CLAUDE.md — Tests

Subtree rules for `tests/`. See the repo-root `CLAUDE.md` for global standards.

Stack: **xUnit v3 4.x on Microsoft.Testing.Platform** (test projects are `OutputType=Exe`; `global.json` puts
`dotnet test` in MTP mode, so there is no VSTest package). Run a single test with
`dotnet test --project tests/<Project> -- --filter-method "*.ClassName.MethodName"` (`--filter-class "*.ClassName"` for
a class, repeatable; `*` only at the start or end). A filter needs `--project`: solution-wide, a project with no
matching test fails the run with exit code 8 (D-063). xUnit's options go after `--`; a solution goes through
`--solution`. A writing session runs its step's classes this way and leaves the full suite to `/novacore-verify` (D-088).

## Two projects

- **`NovaCore.Domain.Tests`** — pure logic of the core abstractions and the modules' domain code (`Result<T>`,
  pagination, `Payment`, `Product`, domain events): no host, no database, no Docker, a few seconds. It is what mutation
  testing runs against (`bash eng/mutation.sh`, below), so a test of that code belongs here. `*PropertyTests` check rules
  over generated inputs and operation sequences with CsCheck (a failure prints the seed that reproduces it:
  `Sample(..., seed: "...")`); give a generator the boundary values too (`Gen.OneOf(Gen.Int, Gen.Int[-2, 3])`), random
  ones rarely hit them.
- **`NovaCore.Api.Tests`** — everything that needs a host, in three flavors below. A property test there that goes to a
  database limits its own samples (`iter`, `threads: 1`, `StockReservationPropertyTests`).

### `NovaCore.Api.Tests`: three flavors

- **Unit** (`Unit/`) — building blocks on a small generic host of their own (`Host.CreateApplicationBuilder`, never the
  Api): startup migrations and the outbox.
- **Integration** (`Integration/`) — boot the real host via `NovaCoreApiFactory` (`WebApplicationFactory`).
  DB-free tests (health, OpenAPI, security headers, 401/validation gates) need no seeding. A caller with specific roles
  comes from `factory.CreateClientWithClaims(...)`: the factory's in-memory issuer (`factory.IdentityProvider`, see
  below) signs the token, so no user is seeded (`ValidationTests`). Permissions are never read from the token, so a
  caller with permissions comes from `factory.CreateClientWithPermissions(...)` (`PermissionAuthorizationTests`). The
  identity provider's own flows are tested on their own host (`IdentityServer/`, below); `IdentityServerWiringTests`
  signs in through the Api host itself.
- **Architecture** (`Architecture/`) — `ModularityTests` enforce boundaries (Payments → only
  Catalog.Contracts; `NovaCore.Abstractions` has no ASP.NET/EF reference). The testable bans in
  `docs/plan/DECISIONS.md` are tests too: banned packages anywhere in the dependency graph (Y-01/32/33),
  core packages on Microsoft packages only plus the one-way package direction (Y-05, `PackageDependencyTests`, which
  reads the `src/BuildingBlocks` csproj files and the built assemblies), module references only via `*.Contracts`
  and no extra per-module projects (Y-08/17), no `IRepository`/`IUnitOfWork` or `*Helper`/`*Util`/`*Manager` types
  (Y-15/29), no assembly scanning or `*FromAssembly*` service registration anywhere under `src/` (Y-20; EF's
  `ApplyConfigurationsFromAssembly` is allowed, D-023), scope + build validation on the test host (Y-26),
  no `*.Domain` types in endpoint signatures via ApiExplorer (Y-31), no TODO/FIXME under `src/` (Y-38), every GitHub
  Action pinned to one full commit SHA across the workflows, the template's included (`WorkflowPinTests`, D-051). `Solution`
  locates the repo root and first-party assemblies.
- **Host wiring** (`Integration/`) — `ModuleRegistrationTests` pin the module order and every module endpoint;
  `ValidationTests` pins the 400 validation ProblemDetails (type, instance, traceId, error keys) of every validated
  request, so a module that forgets `services.AddValidation()` fails here. Update the expected lists when you add or
  change an endpoint.
  **Keep these green** — they are the guardrails for the whole design; never weaken one to pass (Y-46).

## How the test host works (and why)

`ApiFactory` boots the real Program host with what every test host shares (the test issuer, server-side grants,
environment, scope validation, production extras) and lets a subclass choose the databases. Program gives every module
context SQL Server (`AddModuleDatabase`); a later `AddModuleDatabase` replaces it, which is how the test hosts point the
contexts at their databases. Program's SQL Server is then never configured, so each context has one provider, and every
context still goes through `AddModuleDbContext` (its interceptors, the tenant isolation). `NovaCoreApiFactory`, the
default, gives **every DbContext** (Catalog, Payments and the identity provider's) its own **in-memory SQLite** database
(`UseDatabase`, by context type), so the full auth flow and the `/health` database checks run without a database server.
A subclass overrides `UseDatabase` to point one context elsewhere (`HealthEndpointsTests`) or add an interceptor to it
(`StockRaceApiFactory`), and calls the base method for the others.

Each context gets a **named, shareable in-memory database** (`Mode=Memory;Cache=Shared;Pooling=False`, a unique name
per factory). Each context instance opens its own connection to it, and the factory holds one connection open so the
database lives as long as the factory. One connection object shared by every context would break: the host's outbox
dispatcher reads the module databases in the background while requests use them, and a `SqliteConnection` isn't
thread-safe (docs/plan/RESEARCH.md R-069). The Catalog, Payments and identity store schemas are created in the factory
ctor, as after the migrations: the dispatcher's passes read the outboxes, and the identity provider's permission resolver
reads its store for every caller. A test that needs the tenancy store's schema creates it. Outside Development the
factory also brings what a production host must: ephemeral signing/encryption keys for the identity provider (standing
in for certificates), an email sender and a Data Protection key store (`InMemoryKeyStore.AddTo(services)`). A test that
expects another startup failure outside Development adds the key store too, or the missing store is what stops the host.
`HealthEndpointsTests` points one context at a missing file with `Mode=ReadWrite`: the dispatcher opens it too, and the
default mode would create the file.

## The API contract

`OpenApiContractTests` compares the document the host serves (`/openapi/v1.json`) with the committed
`docs/openapi/v1.json`; when they differ it writes the new document there and fails, so an endpoint change brings its
contract change into the same commit (run the tests again, then commit the file). CI's `api-contract` job compares that
file with the base revision's (`eng/check-openapi.sh`, oasdiff): a breaking change fails unless a line of
`eng/openapi-breaking-accepted.txt` accepts it.

## Mutation testing

`bash eng/mutation.sh` (weekly in `mutation.yml`) runs Stryker.NET with its Microsoft.Testing.Platform runner (preview)
against `NovaCore.Domain.Tests`: `NovaCore.Abstractions` and the `Domain/` folders of Catalog and Payments, about two
minutes. The score is a report (`--break-at 0`); the html reports are in `StrykerOutput/<project>/reports`. A surviving
mutant is a change no test noticed. Against `NovaCore.Api.Tests` it is impractical: the runner captures coverage one test
at a time, and those tests start hosts (docs/plan/RESEARCH.md R-116).

## Coverage

`bash eng/check-coverage.sh` (CI's "Test with coverage" step) runs both test projects with coverlet.MTP over the
`NovaCore.*` assemblies (not the tests, not the migrations) and fails when line or branch coverage falls more than its tolerance below
`eng/coverage-baseline.txt`, the coverage main reached. Raise the baseline with `--update` when a change raises the
coverage, and commit it with the change. The architecture tests run in a step of their own, without coverage: coverlet
instruments the built assemblies in place (an instrumented assembly references `netstandard`), and those tests read the
built assemblies' references.

## The database layers (`SqlServer/`, `PostgreSql/`, `Databases/`)

SQLite hides what only a real database does: migrations (the SQLite databases come from `EnsureCreated`) and their raw
SQL, schemas, collations, `decimal` precision, `DateTimeOffset` ordering (docs/plan/DECISIONS.md D-043, D-056). The
packages are tested on SQL Server and PostgreSQL (D-068, D-074):
- `TestSqlServer` and `TestPostgreSql` each start one container for the whole run (Testcontainers, the image
  `docker-compose.yml` pins, which `DatabaseImageTests` keeps equal), when the first test needs it. Fixtures can't depend
  on each other, so each is a static `Lazy`; Testcontainers' resource reaper removes the containers when the test
  process ends.
- `DatabaseApiFactory` is the Api host (`ApiFactory`) in a database of its own on the provider's server
  (`CreateDatabaseAsync`), with `Database:ApplyMigrations` on: the host migrates the four contexts when it starts, the
  way production does. `ResetAsync` empties the module tables between a class's tests (Respawn). `SqlServerApiFactory`
  keeps Program's SQL Server and the host's migrations; `PostgreSqlApiFactory` gives every context PostgreSQL with the
  PostgreSQL migrations of this project (`PostgreSql/<Module>/Migrations`, one set per provider, EF Core). The
  `*AuthorityApiFactory` variants make the host its own `Jwt:Authority`, for signing in through its identity provider.
- **The same scenarios run on both providers:** `Databases/` holds abstract generic classes (`MigrationScenarios`,
  `OutboxScenarios`, `QueryScenarios`, `SignInScenarios`); `SqlServer…` and `PostgreSql…` classes derive from them with
  their factory. A scenario that holds on every provider goes there; what one provider does differently (SQL Server's
  migration data steps, each server's default collation in the product search) stays in that provider's class.
- **Tenant isolation** (`TenancyScenarios`) runs on SQLite too (`SqliteTenancyTests`, no Docker). Its host is
  `CoreWebTestHost` with a module context of tenant entities (`TenancyFixture`'s `NotesDbContext`, tables from
  `EnsureCreated`), not the Api host, whose modules have none. Its roles (`Writer`, `Reader`) grant the notes module's
  permissions in the member's tenant, and `/api/v1/tenants/{tenant}/permissions` echoes the caller's there. A note added
  over HTTP raises `NoteAdded`; the host's outbox dispatcher runs its handler on the real clock (a pass every five
  seconds), and `TenancyFixture.HandledAsync(text)` waits for what it saw (its tenant, the tenant's notes, its trace). A
  test that needs the request's trace sends a `traceparent` and listens to that trace only: listeners are process-wide.
  A `Reminder` points at a note through an optional relationship, which the database doesn't cascade: the tenant
  deletion scenario shows that a dependent goes first. A deleted tenant is gone, so a deletion test opens its own.
  Each test works with tenants of its own (`Guid.NewGuid()`), so a class shares one database without resets. `TenantConvention`
  is the convention test: an entity with a `TenantId` column must carry the tenant filter (`ModuleRegistrationTests`
  runs it on the Api host's contexts). `TenantPermissionTests` (under `IdentityServer/`) runs the identity provider and
  the add-on on one host: the Administrator rule with a membership.
- Raw SQL that runs on both quotes its names with the context's provider (`PortableSql`: `[catalog].[outbox_messages]`
  vs `"catalog"."outbox_messages"`; an unquoted name folds to lower case on PostgreSQL) and passes values as parameters.
  `COUNT(*)` is an `int` on SQL Server and a `bigint` on PostgreSQL: cast it (`CAST(COUNT(*) AS INTEGER)`).
- A model change needs a migration in the PostgreSQL set too (root `CLAUDE.md`); `PostgreSqlModelTests` fails until it
  has one, without Docker. A test that needs an empty or a half-migrated SQL Server database creates its own
  (`MigrationTests` migrates step by step with `IMigrator`, on the host's design-time factories).
- Without Docker these tests skip (`TestDatabase.SkipIfUnavailable()` first in each test). CI and release run
  `dotnet test ... -- --fail-skips on`, so there a skip fails the run.

## Background work: the outbox dispatcher

`OutboxDispatcherTests` drive the dispatcher on a small host (`AddCorePersistence` + a sample context) with a
`FakeTimeProvider`:
- The first pass runs when the host starts; every clock advance past the 5-second interval makes another.
- Save the first messages **before** starting the host, then wait for their effect. Once the first pass has shown up,
  the dispatcher's `PeriodicTimer` exists and no later advance is lost: ticks during a pass are kept, not dropped.
- Wait on visible effects (the outbox row, the handler log), polling with a timeout. One advance can yield two passes,
  so assert "at least" or keep advancing until the end state; to show that a pass ran, save a later "barrier"
  message and wait for it.
- Don't advance the clock by days: `FakeTimeProvider` fires a periodic timer once per period it moves over, so 7 days
  of a 5-second timer is 120,000 callbacks (about a minute). Age rows by their timestamps instead
  (`AddProcessedMessageAsync` for the retention tests).

On the Api host the dispatcher runs too. With `FakeClockApiFactory` its timer follows the fake clock:
`OutboxDelivery.RunUntilAsync(factory.Clock, condition)` advances it by the polling interval until the effect shows up
(`StockReservationTests` runs a payment from POST to Completed this way). A module's handler can also be called
directly with `factory.HandleAsync(event)` (`ModuleTestData`), the way the dispatcher calls it.

A pass may be running whenever a test acts: the first pass at startup, or one queued by an earlier test's clock
advances in the same class fixture. So:
- a test that hands a request to a handler itself saves it with `AddPaymentForDirectHandlingAsync`, which keeps the
  request out of the outbox;
- a test that needs several requests past `CreatePayment`'s check saves them with `AddPaymentAsync` instead of racing
  POSTs against that pass.

SQLite keeps `DateTimeOffset` as text and EF Core can't compare or order it there (R-068), so every SQLite test context
stores it as UTC ticks instead (`UtcTicksModelCustomizer`, through `ReplaceService<IModelCustomizer,
UtcTicksModelCustomizer>()` in `NovaCoreApiFactory.UseSqlite` and on the small hosts): production code compares and orders
`DateTimeOffset` in SQL, as it should on a real database (the outbox retention, the payment list). Raw SQL against those
columns writes and reads ticks (`OutboxDispatcherTests`). `QueryScenarios` tests the payment list on SQL Server and
PostgreSQL.

`StockRaceApiFactory` (a `FakeClockApiFactory`) plays a concurrent writer for `StockConcurrencyTests`.
`ChangeStockBeforeNextSave(productId, stock)` makes the next Catalog save that modifies or deletes the product first
see another connection set its stock. The change lands between the save's read and its write. An update that changes
nothing sends no `UPDATE`, so the request must really change the product.

## Tokens: an in-memory OpenID Connect issuer

The core validates access tokens against `Jwt:Authority` (D-002, D-024). `TestIdentityProvider` is that authority for
tests: an in-memory RSA key, with the discovery document and JWKS served by an `HttpMessageHandler`. The factory sets
`Jwt:Authority` to `TestIdentityProvider.Issuer` in every environment and wires the handler as
`JwtBearerOptions.BackchannelHttpHandler` (in `Configure`: JwtBearer builds the backchannel in `PostConfigure`, R-056).
So tokens go through the real Authority → metadata → JWKS path with no network.

- `factory.IdentityProvider.CreateToken(claims)` signs with the published key.
- `TestIdentityProvider.CreateToken(claims, key, algorithm, issuer, audience, expires)` breaks one property at a time
  for rejection tests (`JwtValidationTests`).
- **Permissions** are resolved on the server (`IPermissionResolver`), and the core asks every registered resolver. The
  factory adds one: a subject of `IdentityProvider`'s tokens holds what `factory.GrantPermissions(subject, ...)` gave it
  (nothing by default), on top of what the host's resolvers grant it (nothing: the identity provider's store doesn't
  know it). The add-on's own tokens hold what its store says. `CreateClientWithPermissions(...)` does both for a fresh
  subject.
- Resolved permissions are cached (`Cache:PermissionExpiration`). `FakeClockApiFactory` drives the memory cache's
  clock too (`MemoryCacheOptions.Clock`: HybridCache's local entries expire by it, not by `TimeProvider`, R-057), so
  `PermissionResolutionTests` moves the clock past the lifetime instead of waiting. **Gotcha:** the identity provider
  stamps its tokens from the fake clock while JWT validation reads the real one (R-058), so never sign in through it
  after a shared fixture's clock was advanced (the token would be "not yet valid", 401). That is why the fake-clock
  classes under `IdentityServer/` each have their own host.

## The identity provider add-on: its own host

`IdentityServer/` tests `NovaCore.Identity.OpenIddict` on `IdentityServerTestHost`: a `WebApplication` composed only of
the core packages and the add-on (no modules, no Api project), on TestServer at `https://localhost` with an in-memory
SQLite store. It reads only its own in-memory configuration: it drops the JSON sources, since the test output also
holds the Api project's `appsettings` files, whose Identity section names the Api's modules. The store is a named,
shared in-memory database and every context instance opens its own connection, like the Api factory's: the add-on's
token pruner reads it in the background (a pass at startup, then hourly) while requests do. That address is also
`Jwt:Authority`, and JwtBearer's backchannel goes to the same server, so the add-on's tokens are validated through its
own discovery document and keys. The add-on sends emails in the background after the response (`EmailQueue`, F6b.6a),
so a test waits for one with `CapturingEmailSender.WaitForAsync`; to show an email did *not* go, it queues a later one
and waits for that first (they go out in the order queued). `OidcFlow` plays the browser (cookies, no automatic
redirects) and the public client: authorize → sign-in form (antiforgery token) → code → token endpoint. Other forms
(password reset, the sign-out question) go through `OidcFlow.SubmitFormAsync`, which posts a page's hidden fields with
the given ones. `TokenPruningTests` moves a fake clock by an hour to run a pruning pass; it signs nobody in.
`TwoFactorRaceTests` makes two requests verifying one authenticator code interleave on every run: its host's store
contexts carry `UsedCodeRace`, which runs the competing request to its end just before the next save that records a
used code, so both read the user before either writes.
`IdentityServerFixture` shares one host per class; tests that need a different host start their own
(`IdentityServerTestHost.StartAsync(environment, configure)`). These classes run in the `IdentityServerTestGroup`
collection, apart from the parallel tests: their CPU load made the `WebApplicationFactory` startup race (R-055) hit
most full runs.

## The core alone: `CoreWebTestHost`

`CoreWebTestHost` is a host composed only of the core web package (`AddCoreWeb`, a resolver that grants nothing,
`UseCoreWeb`) whose endpoints each test maps itself: an endpoint without authorization metadata, one that echoes the
request, one that throws. Tokens come from its own `TestIdentityProvider` (`CreateClientWithClaims`). Use it for
pipeline behaviour the Api host can't show because its endpoints are fixed (`SecureDefaultsTests`).

## Test configuration — in-memory config is enough

The building blocks bind configuration through the options pattern and read it when the options are first built,
not during registration (docs/plan/RESEARCH.md R-048). So `NovaCoreApiFactory`'s `ConfigureAppConfiguration` values
(the test authority, the Development signing key, migrations off, seeding off, a high rate limit) reach the app like
any other configuration. The high limit keeps the many `/auth` calls from tripping the `"strict"` policy (which would
return 429). Don't set environment variables in tests: they are process-wide and race the other test classes.

**One factory needs a different value?** Subclass `NovaCoreApiFactory`, override `ConfigureWebHost`, call `base` and
then add another `ConfigureAppConfiguration`: sources added later win. Examples: `StrictRateLimitApiFactory`, and the
factory in `ConfigurationValidationTests`. Invalid values stop the host on start (`ValidateOnStart`). Assert that on a
host started directly (`CoreWebTestHost.StartAsync`, `IdentityServerTestHost.StartAsync`), not on `factory.Server`: a
`WebApplicationFactory` host that fails to start can throw `ObjectDisposedException` instead of the startup error
(R-055, R-109, dotnet/aspnetcore#69684).

**Different environment?** Override `EnvironmentName` (default `Development`), e.g. `ProductionApiFactory`.

`xUnit1051` (CancellationToken) is `NoWarn`'d for these simple `HttpClient` tests.

Warnings are errors here too. Test names are sentences with underscores (`Refresh_rotates_the_refresh_token`), so
`.editorconfig` turns CA1707 off for `tests/**.cs` only (DECISIONS D-011). Every other analyzer rule applies, the
Usage and Reliability categories at `latest-all` and Meziantou.Analyzer included (D-064, D-066): pass `HttpClient` a
`Uri` (`new Uri("/api/v1/products", UriKind.Relative)`, CA2234), dispose request contents and messages (CA2000),
compare strings and key string collections with `StringComparer.Ordinal`/`StringComparison.Ordinal` (MA0002, MA0006),
and give a `Regex` a timeout (MA0009). No `ConfigureAwait` in tests: CA2007 and MA0004 are off here, and xUnit1030
forbids `ConfigureAwait(false)` in a test method.
