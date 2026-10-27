# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

NovaCore is a **.NET 10 single-developer modular-monolith Web API** starter. Deep design rationale
lives in `README.md`; this file is the operating manual — commands, the big-picture architecture, and
the **non-negotiable standards** to follow when changing code here.

---

## ⚠ Active restructuring: plan execution protocol (read first)

NovaCore is mid-way through a multi-phase restructuring. Its source of truth lives in `docs/plan/`
(written in Turkish). **Precedence:** `DECISIONS.md` > `PLAN.md` > `PROGRESS.md` > this file > the conversation.
Where this file's Architecture/Standards sections disagree with `DECISIONS.md`, `DECISIONS.md` wins;
completed phases update this file.

- **Phase work starts only via `/novacore-phase <id>`** (project skill, user-invoked), never two phases in one
  session. A phase spans several sessions: after at most three committed steps the skill pauses (`⏸`) and the user
  continues with `/clear` and the same command, without approving the plan again (D-082). When a phase ends,
  **stop**. "devam"/"continue" means "next step inside the approved phase plan", never "start the next phase".
- **Each writing session is followed by a verifying one (D-088):** `/clear`, then `/novacore-verify <id>` (project
  skill, user-invoked). The writing session runs only the build, the step's new or changed test classes (red, then
  green; `--project` + `--filter-class`, the SQLite class of a multi-database scenario) and `dotnet format`; the
  verifying session runs the full suite and `code-review`, writes findings to PROGRESS and fixes no code.
  `/novacore-phase` stops at a code commit no verify has seen and closes open findings before a new step. At the
  phase end the last verify runs the phase verification and the CodeRabbit CLI, opens the `faz/...` → `main` pull
  request that CodeRabbit reviews (advisory, `.coderabbit.yaml`, D-067), answers out-of-scope and false-positive
  comments and asks for the merge; `/novacore-phase` fixes the in-scope findings and pushes them.
- **Never implement anything in DECISIONS.md → "Yapılmayacaklar"**, not even as a design suggestion. If one
  seems necessary, stop and ask with the reason and a source.
- **Counter the "most typical answer" pull (Y-40…Y-43, applies to every session, not only phase work):**
  every proposed pattern/abstraction/package names the concrete problem it solves *in this codebase*
  (file/line), one simpler alternative and why it falls short, and a primary source. When asked to choose
  anything (design, naming, even a color), offer 2–3 genuinely different options with trade-offs and a
  project-specific recommendation. "Best practice"/"commonly done" is never a reason on its own. Never
  re-propose a rejected approach under a new name; say "I propose reopening Y-xx" instead.
- **Make technical decisions yourself (DECISIONS D-016).** Don't guess: research the primary source, chosen by
  topic (D-079). Microsoft technology: Microsoft Learn MCP only (`microsoft_docs_search` / `microsoft_docs_fetch` /
  `microsoft_code_sample_search`), plus SDK files and `dotnet msbuild -pp`. A third-party package: find it with
  Context7, then cite the package's official repo or docs in the R-entry, not Context7 (WebFetch the repo when
  Context7 is unavailable). Package versions, advisories and supply chain: NuGet MCP's read-only tools, never its
  update tools. If Microsoft's docs say how to do it, do it that way. If they don't,
  weigh cost (implementation + maintenance + risk) and pick the sensible option. Then log it (RESEARCH R-entry,
  a new DECISIONS D-entry if it lasts, PROGRESS note) and report it; don't ask. Writing plan docs needs no
  permission.
- **Ask (`AskUserQuestion`) only for the user's decisions:** contradicting or reopening a D/Y record, a
  product/ownership choice the PLAN leaves to the user, a new non-Microsoft package, or an outward/irreversible
  action (merge, push, tag, publish, delete).
- **Another phase's topic?** Read that phase in PLAN.md first. Do the proper work now if it's needed now and
  cheaper overall (record a D-entry); otherwise leave the code alone and note it in PROGRESS →
  "Sonraya bırakılanlar". **No temporary code or suppressions** ("remove in phase X"); they get forgotten (Y-45).
- **Hooks are guardrails, not obstacles** (`.claude/settings.json`): new non-allowlisted NuGet packages, edits
  to `PLAN.md`, `DECISIONS.md`, `.claude/settings.json` and `.claude/hooks/`, a workflow `uses:` not pinned to a commit
  SHA, and a written value that looks like a secret trigger a user approval prompt.
  `DECISIONS.md` prompts even for a new entry; policy-wise new entries are pre-approved (D-016), so batch them
  into one edit per phase step. Banned packages are denied, and the Stop hook blocks finishing while the build
  is red (it runs no tests: the full suite is `/novacore-verify`'s, D-088). Don't try to route around them. The auto-mode classifier also blocks the model from editing hooks.
- Outside the restructuring (a quick question, a one-off fix the user asks for), work normally, but still
  respect DECISIONS.md.

### Working habits (from the Ponytail skill, DECISIONS D-048 (4))

- **Use the .NET tooling while coding (D-079)**: find a symbol's callers with the C# LSP (`findReferences`,
  `incomingCalls`, `goToImplementation`) and keep grep for text, configuration and the uncompiled template content.
  Open the matching .NET skill (`dotnet-data`, `dotnet-test`, `dotnet-aspnetcore`, `dotnet`) as a checklist;
  DECISIONS, PLAN and this file win over a skill. The `dotnet-test` agents only run self-contained audits (test gaps,
  test quality), never phase code.
- **Before an edit, list every place it must reach**: the callers (LSP, see above), the tests and fixtures, the
  configuration, the docs that describe it, and the template's copies under `src/Templates/.../content`, which this
  repository doesn't compile (only `bash eng/template-smoke.sh` does).
- **A bug fix starts with the callers**: find every caller of the code you touch (LSP, grep for text) and fix the root cause once, in the
  shared code, instead of at each call site.
- **End a report with what you skipped or did not check**, and any risk the user must know, in a line or two.

---

## Commands

```bash
# Build / test
dotnet build                                  # whole solution (NovaCore.slnx); TreatWarningsAsErrors: any warning fails the build
dotnet restore NovaCore.slnx --force          # incremental builds skip restore, so NuGet audit (NU19xx) stays hidden without this
dotnet restore NovaCore.slnx --force-evaluate # after changing a package version: rewrites the packages.lock.json files
dotnet restore NovaCore.slnx --locked-mode    # what CI runs: fails if the package graph differs from the lock files
bash eng/check-licenses.sh                    # license gate: every package MIT/Apache-2.0; exceptions: Microsoft, Polly.* (transitive, D-052), Npgsql and Npgsql.EntityFrameworkCore.PostgreSQL (D-072)
bash eng/check-coverage.sh [Debug] [--update] # tests with coverage, held to eng/coverage-baseline.txt (D-060); build first
bash eng/check-openapi.sh [base-revision]     # breaking changes in docs/openapi/v1.json since main (oasdiff, Docker; D-061)
bash eng/mutation.sh                          # Stryker.NET on the abstractions and domain code, a report (D-063)
bash eng/lint-workflows.sh [dir ...]          # actionlint + zizmor on .github (Docker; GH_TOKEN enables online audits)
bash eng/find-unused.sh                       # unused internal/private types, fields, constants (Roslynator, D-066)
coderabbit review --agent --base main         # CodeRabbit CLI review of the phase branch, once at phase end in /novacore-verify (D-067, D-082, D-088; `coderabbit auth login` first)
dotnet format NovaCore.slnx                   # fixes what CI's `dotnet format --verify-no-changes` reports
git config core.hooksPath .githooks           # opt-in pre-commit secret scan (needs Betterleaks 1.9.0 on PATH)
dotnet build -c Release
dotnet test                                   # all tests: SQLite, plus the SQL Server and PostgreSQL layers in Docker containers (skip without Docker)
dotnet test --project tests/NovaCore.Api.Tests -- --filter-method "*.AuthorizationCodeFlowTests.Refresh_rotates_the_refresh_token"   # single test (--filter-class for a class)
#   A filter needs --project: solution-wide, a project with no matching test fails the run with exit code 8 (D-063).
#   `dotnet test` runs on Microsoft.Testing.Platform (global.json): the solution goes through --solution, xUnit's own
#   options come after `--`, and -nologo isn't accepted before .NET 11 (exit code 5).

# Run (Development opens Scalar UI at /scalar/v1)
dotnet run --project src/Api/NovaCore.Api

# EF Core migrations — FOUR separate DbContexts: one per module, the identity provider's and the tenancy store's. The
# host chooses the database (AddModuleDatabase) and keeps every context's SQL Server migrations and design-time factory (D-073), so the
# Api is BOTH -p and -s. Restore tools first.
dotnet tool restore
dotnet ef migrations add <Name> --project src/Api/NovaCore.Api --startup-project src/Api/NovaCore.Api --context <C>DbContext --output-dir <F>/Migrations
dotnet ef database update     --project src/Api/NovaCore.Api --startup-project src/Api/NovaCore.Api --context <C>DbContext
#   <C>/<F> ∈ { Catalog/Catalog, Payments/Payments, IdentityServer/Identity, MultiTenancy/MultiTenancy }.
# A model change needs a migration in each provider's set (D-074): the PostgreSQL test layer's lives in the test project.
dotnet ef migrations add <Name> --project tests/NovaCore.Api.Tests --startup-project tests/NovaCore.Api.Tests --context <C>DbContext --output-dir PostgreSql/<F>/Migrations

# The template package (src/Templates/NovaCore.Templates, package BMD.NovaCore.Templates) and its smoke test: packs every
# package into a local feed, creates a project for each --auth (BuiltIn, External), --db (SqlServer, PostgreSql) and
# --multitenancy (false, true), builds, tests, runs its license gate and checks its migrations.
bash eng/template-smoke.sh [work-dir]
dotnet new install BMD.NovaCore.Templates::<version>    # from the GitHub Packages feed
dotnet new novacore -n MyProject [--auth BuiltIn|External] [--db SqlServer|PostgreSql] [--multitenancy]

# NuGet packages (the eight building blocks + the template, IDs BMD.NovaCore.*) → artifacts/ (git-ignored). Version: VersionPrefix.
dotnet pack NovaCore.slnx -c Release -o artifacts [--version-suffix preview.1]
```

There is no separate lint step: every build runs the analyzers with `EnforceCodeStyleInBuild=true` and
`TreatWarningsAsErrors=true`: `latest-recommended`, every Security, Reliability, Performance and Usage rule
(`AnalysisLevel<Category>=latest-all`; `AnalysisMode<Category>` would be ignored, R-117), the dead-code IDE rules,
Microsoft's code metrics, BannedApiAnalyzers under `src/` (`src/BannedSymbols*.txt`) and Meziantou.Analyzer (D-064…D-066).
CI also runs `dotnet format NovaCore.slnx --verify-no-changes`; run `dotnet format NovaCore.slnx` to fix what it reports. Publishing is `.github/workflows/release.yml`
on a `v*` tag that must match `VersionPrefix` (GitHub Packages): a read-only `build` job, then a `publish` job in the
`release` environment (owner approval) that signs build provenance and pushes. CI also runs CodeQL (`codeql.yml`),
Scorecard (`scorecard.yml`), a Betterleaks history scan and the workflow linters; the SDK is pinned exactly
(`global.json` `rollForward: disable`, D-048).
The Api host chooses **SQL Server** for every module context (`AddModuleDatabase` in `Program.cs`); its connection string
targets a **local SQL Server via Windows auth** (`appsettings.json`).

---

## Architecture (the big picture)

**Hybrid: modular monolith (macro) + vertical slice (micro) + a thin shared core.** Read these together:

- **Packages (`src/BuildingBlocks/`, ANALYSIS §4):** three **core** packages that depend on the BCL and Microsoft
  packages only (D-003) — `NovaCore.Abstractions` (BCL only), `NovaCore.AspNetCore` and `NovaCore.EntityFrameworkCore`
  — plus optional **add-on** packages for third-party libraries: `NovaCore.Serilog`, `NovaCore.OpenTelemetry` (logs,
  metrics, traces), `NovaCore.Scalar`, `NovaCore.Identity.OpenIddict` (the OpenID Connect provider),
  `NovaCore.MultiTenancy` (tenants, memberships, their roles and the life cycle).
  **Package IDs are `BMD.NovaCore.*`** (`Directory.Build.targets`: `BMD.$(MSBuildProjectName)`); assembly, project and
  namespace names stay `NovaCore.*` (D-041: other owners publish `NovaCore.*` on nuget.org).

- **The template (`src/Templates/NovaCore.Templates`, package `BMD.NovaCore.Templates`, D-040):** `dotnet new novacore`
  creates a solution that references the packages of the template's own version (stamped into its
  `Directory.Packages.props` when packing). It compiles nothing here; its `.cs` files are still scanned by the
  architecture tests under `src/`. Change it, then prove it with `bash eng/template-smoke.sh` (see
  `src/Templates/CLAUDE.md`).

- **Dependency direction (one way):** `Api` → `Modules.*` → `NovaCore.AspNetCore` / `NovaCore.EntityFrameworkCore`
  → `NovaCore.Abstractions` → BCL. AspNetCore and EntityFrameworkCore never reference each other; add-ons reference
  the core packages only, never another add-on (`NovaCore.MultiTenancy` uses both AspNetCore and EntityFrameworkCore,
  D-076 (2)). Enforced by `PackageDependencyTests`.

- **The host composes everything explicitly** (`src/Api/NovaCore.Api/Program.cs`): the add-ons (`AddCoreLogging`,
  `AddCoreObservability`), the core (`AddCoreDefaults`, `AddCoreWeb`, `AddCorePersistence`), the database of every
  module context (`AddModuleDatabase`: SQL Server, the host's migrations), the identity provider
  (`AddCoreIdentityServer` + `AddModuleDbContext<IdentityServerDbContext>`), multi-tenancy (`AddCoreMultiTenancy` +
  `AddModuleDbContext<MultiTenancyDbContext>`), then one `builder.AddModule<TModule>()` per module; after `Build`,
  `UseSerilogRequestLogging`, `UseCoreWeb`, `MapCoreScalar`, `MapCoreIdentityServer`, `MapModules`,
  `MapDefaultEndpoints` (health endpoints, Development only, D-022). Nothing is found by
  assembly scanning (Y-20, `ExplicitRegistrationTests`).

- **Modules are self-contained.** Each `IModule` registers its own services + DbContext + permissions + request
  validation (`services.AddValidation()`) and maps its own endpoints. **Adding a module = new project + implement `IModule` + reference it from `Api` + one
  `builder.AddModule<TModule>()` line in `Program.cs`.** Modules map their endpoints in that order. (See
  `src/Modules/CLAUDE.md`.)

- **Cross-cutting lives in the core:** `AddCoreDefaults` (health + HttpClient resilience) and
  `AddCoreWeb`/`UseCoreWeb` (ProblemDetails + exception handlers, OpenAPI document, JWT auth, permission authz, rate
  limiter, CORS, security headers) in `NovaCore.AspNetCore`; logging, tracing and the Scalar UI are add-ons.

- **Persistence:** each module owns a `DbContext` pinned to its own schema (`catalog` / `payments` / `identity`, via
  `HasDefaultSchema`). Register through `services.AddModuleDbContext<TContext>(schema, configure)`
  (`NovaCore.EntityFrameworkCore`): it keeps the context's migration history in its schema, adds the outbox interceptor and
  enrolls the context for generic startup migration (`DatabaseMigrationService`, registered by `AddCorePersistence`; it
  migrates only when `Database:ApplyMigrations` is true). **No module chooses a database:** the host gives every module
  context its provider once, `services.AddModuleDatabase((sp, options) => options.UseSqlServer(...))` (a later call
  replaces it; without one the host doesn't start), and keeps the provider-specific migrations and design-time factories
  (`src/Api/NovaCore.Api/{Catalog,Payments,Identity}`, D-068, D-073). `ModuleProviderTests` checks that no module
  references a provider. The packages are tested on SQL Server and PostgreSQL (D-074).

- **Domain events go through a transactional outbox.** An entity `Raise`s a `DomainEvent` record; the save that saves
  the entity writes it to the module's `outbox_messages` in the same transaction (`OutboxInterceptor`, added by
  `AddModuleDbContext`; the context calls `modelBuilder.AddOutbox()`). `OutboxDispatcher`, a `BackgroundService` from
  `AddCorePersistence`, delivers the events to their `IDomainEventHandler<T>`s: each message in its own DI scope (in the
  tenant of the scope that saved it, or in none) and its own consumer span (continuing the saving trace, D-081), each
  handler once (`outbox_consumers` records the finished ones, keyed by the message row), retried on later passes, left
  for inspection after 10 failures; processed messages are deleted with their records after 7 days. Delivery is at
  least once, so handlers must be idempotent; one dispatcher per database (D-031, D-036 (7), R-068). The outbox is the
  only event path: there is no in-process dispatcher.

- **Security** is a first-class subsystem. The core only *validates* access tokens (D-002): JWT bearer against an
  OpenID Connect provider, `Jwt:Authority` (issuer + JWKS discovered, asymmetric algorithms only), required in every
  environment (D-028). Permission-based authorization lives in
  `NovaCore.AspNetCore` too: permissions are resolved on the server through every registered `IPermissionResolver`
  (never read from the token; the caller holds what any of them grants) and cached per `sub` + security stamp +
  requested tenant for `Cache:PermissionExpiration`; code that changes what a resolver returns drops the entries at once
  with `HybridCache.RemovePermissionsAsync(sub)` (a tag). The core's claims transformation is the only one: another
  `IClaimsTransformation` stops the host (`ClaimsTransformationStartupCheck`). The tokens come from the
  `NovaCore.Identity.OpenIddict` add-on (ASP.NET Core Identity + OpenIddict: Authorization Code + PKCE, refresh
  rotation with an absolute 7-day lifetime, client credentials, single-use TOTP 2FA with hashed recovery codes,
  password reset, token pruning, admin; the API accepts only its `at+jwt` tokens), which the Api host runs as its own
  authority (DECISIONS D-037, Faz 6b: D-053…D-055). See `src/BuildingBlocks/NovaCore.Identity.OpenIddict/CLAUDE.md` before
  touching auth.

- **Multi-tenancy** (D-069, D-076…D-084; README "Çok kiracılılık"): tenants share one database and their rows carry a
  `TenantId`. The core isolates every `ITenantEntity` in every module context with nothing for the module to do
  (`AddModuleDbContext`: a named query filter, `TenantQueryFilter.Name`; a write guard; a parameterless
  `IgnoreQueryFilters()` fails), in the tenant of the DI scope (`TenantContext`, given once; none → no rows). A request
  gets its tenant from the optional `NovaCore.MultiTenancy` add-on (`AddCoreMultiTenancy`: the `tenant` route value,
  validated against the caller's membership and the tenant's status on the server, cached with the permissions; no
  tenant claim in the token, D-070 (3)); `RequireTenant()` guards a tenant endpoint. Members' roles grant permissions in
  their tenant (`MultiTenancy:Roles`), the outbox runs a message's handlers in the tenant that saved it, and
  `TenantDirectory` runs the life cycle (active, suspended, closed, deleted through `TenantData`). There is no "see
  everything" switch: other work runs in a tenant's scope, a read across tenants turns the filter off by its name.
  Row-level security is a documented example for the host, not package code (D-069 (5)). The Api host wires the add-on;
  the repo's modules have no tenant entities, so isolation is tested on `TenancyFixture` (`tests/CLAUDE.md`).

---

## Standards (do / don't)

These are hard rules. Breaking them defeats the point of the project or fails an architecture test.

- **Packages: MIT/Apache (or Microsoft built-in) ONLY**, plus the named exceptions DECISIONS records (Y-47: `Polly.*`
  transitively; a project's database provider and driver by exact package ID, `eng/check-licenses.sh`). **Never add MediatR, AutoMapper, or
  FluentAssertions** — they are now commercial. Use: Mapperly (mapping), .NET 10's built-in validation
  (validation), Scalar (OpenAPI UI). No other mediator or request-dispatch abstraction either (DECISIONS Y-01).

- **No mediator / no CQRS dispatcher.** Handlers receive dependencies directly from minimal-API DI
  (`sade CQRS`). Don't introduce a request-dispatch abstraction.

- **`NovaCore.Abstractions` stays framework-free** (BCL only — no ASP.NET, no EF), and the core packages depend on
  Microsoft packages only; third-party libraries go into an add-on package. Enforced by `ModularityTests` and
  `PackageDependencyTests`. Put anything needing ASP.NET Core in `NovaCore.AspNetCore` and anything needing EF Core in
  `NovaCore.EntityFrameworkCore`.

- **Modules never reference each other directly.** Cross-module calls go through a `*.Contracts` project
  (in-process interface, e.g. `IProductCatalog`) or a domain event — never another module's DbContext.

- **Expected failures → `Result`/`Result<T>` + `.ToHttpResult()`** (`NovaCore.AspNetCore`). Reserve
  exceptions for the genuinely exceptional. Handlers return typed results (`Task<Results<Ok<T>, ProblemHttpResult>>`,
  never a bare `IResult`): the return type is the response contract in the OpenAPI document, so don't add
  `.Produces<T>()` for the success response; declare the problem statuses with `.ProducesProblem(...)` (F4c.5). Domain exceptions live in `NovaCore.AspNetCore/Exceptions`.

- **Endpoints are guarded by permissions, not ad-hoc role checks:**
  `endpoint.RequirePermission("module:resource:action")`. Authorization is closed by default: an endpoint without
  authorization metadata requires an authenticated caller (fallback policy, F4c.1), and a public one says
  `.AllowAnonymous()`. Declare a module's permissions via
  `IPermissionDefinitionProvider` + `AddPermissionDefinitions<T>()`. No token role bypasses a permission check: the
  identity provider's resolver gives an `Administrator` member every declared permission, on the server (F4c.2).

- **GitHub Actions are pinned to a full commit SHA** with the release in a comment (`uses: actions/checkout@<sha> # v7.0.1`),
  resolved with `gh api repos/<owner>/<repo>/git/ref/tags/<tag>`. Workflows grant `permissions: {}` and each job only
  what it needs; checkouts use `persist-credentials: false`. The template's workflow pins the same SHAs as the
  repository's (`WorkflowPinTests`); Dependabot updates only the repository's, so update the template's with it.

- **Central Package Management:** versions live ONLY in `Directory.Packages.props`. `*.csproj` use
  versionless `<PackageReference>`. Common build props are in `Directory.Build.props` (net10.0, nullable,
  implicit usings, analyzers, version); package metadata for `IsPackable` projects is in `Directory.Build.targets`;
  per-project csproj stay tiny. Non-packable projects (the Api host, the modules, the tests) commit a
  `packages.lock.json` (`RestorePackagesWithLockFile`, set in `Directory.Build.targets`); after a version change run
  `dotnet restore NovaCore.slnx --force-evaluate` and commit the lock files with it.

- **0 warnings is enforced** (`TreatWarningsAsErrors`). Fix a warning; don't suppress it. Suppressing (`NoWarn`,
  `#pragma`, `.editorconfig` severity, `[SuppressMessage]`) needs user approval (DECISIONS Y-46), unless Microsoft's
  "When to suppress" section for that rule allows it permanently. The approved ones are
  listed under Gotchas.

- **Configuration goes through validated options, never read at registration.** Bind a section with
  `AddOptions<T>().BindConfiguration(section).ValidateOnStart()`, put the rules on the options type as DataAnnotations
  with an `[OptionsValidator]` partial validator (rules attributes can't express go in `.Validate(...)`), and configure
  framework options that depend on it with `AddOptions<TFramework>().Configure<IOptions<T>>(...)`. No
  `configuration.GetValue`/`Get<T>` while registering services: values added after `CreateBuilder` (tests, hosts)
  would be missed (docs/plan/RESEARCH.md R-048).
- **Time comes from an injected `TimeProvider`**, never `DateTime(Offset).UtcNow`/`Now` (`SourceHygieneTests`
  enforces it under `src/` by text, BannedApiAnalyzers by symbol everywhere but `NovaCore.Abstractions`). The building blocks register `TimeProvider.System` with `TryAddSingleton` next to their
  consumers; tests swap in `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`). The one exception is the
  `DomainEvent.OccurredOnUtc` default (DECISIONS D-021).
- **Logging is source-generated** (CA1848 is an error): `[LoggerMessage]` partial methods, explicit `EventId`s unique
  within the assembly (SYSLIB1006). Default to instance methods on the class's primary-constructor `ILogger`. When an
  argument must be computed (CA1873), wrap the call in an `IsEnabled` guard and declare the methods as `this ILogger`
  extensions: CA1873 only accepts a guard on the same receiver (`SecurityAuditLogger`, RESEARCH R-047).

- **Packable projects are NuGet packages** (the three core packages and the five add-ons in `src/BuildingBlocks`):
  - Every public member needs an XML doc comment (CS1591).
  - The `.csproj` sets `<GenerateDocumentationFile>true</GenerateDocumentationFile>` next to `IsPackable` and references
    `Microsoft.CodeAnalysis.PublicApiAnalyzers` (`PrivateAssets="all"`); `Directory.Build.targets` fails the build
    without either. Add `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` starting with `#nullable enable`.
  - A public API change fails the build (RS0016/RS0017) until `PublicAPI.Unshipped.txt` is updated: use the IDE code
    fix or `dotnet format analyzers <csproj> --diagnostics RS0016 --severity warn`. `PublicAPI.Shipped.txt` stays
    empty until the first release is declared (F5.4).
  - Only one public overload of a method may take optional parameters (RS0026); prefer plain overloads.
  - Keep types `internal` unless consumers need them (DECISIONS Y-23).

- **Vertical slice:** one feature = one file (request + response + validation + route + handler together)
  under a module's `Features/`. Don't spread a feature across layered folders.

- **Validation is .NET 10's built-in Minimal API validation.** Put DataAnnotations on the request record's
  parameters (`[Required]`, `[MaxLength]`, `[Length]`, `[Range]`, `[EmailAddress]`). A rule no attribute expresses is
  a custom `ValidationAttribute` when it checks one value, or `IValidatableObject` when it spans several properties
  (that runs only once every property is valid). Every module calls `services.AddValidation()` in `AddModule`: the
  source generator only covers the assembly that makes the call, and a missing call fails silently, letting invalid
  input reach the handler (docs/plan/RESEARCH.md R-052). `ValidationTests` pins every validated endpoint.

- **Mapping:** Mapperly static partial mappers (`[Mapper]`). For EF list queries use a source-generated
  `ProjectToDto` (IQueryable projection) so it translates to SQL.

---

## Gotchas (learned the hard way)

- **`WithTags` on a route group** needs `using Microsoft.AspNetCore.Http;` (not just `...Builder`).
- **Provider-neutral code:** modules and packages use EF Core's provider-neutral API (`EF.Functions.Like`, not Npgsql's
  `ILike`); the host's `AddModuleDatabase` (and its design-time factories) is the only `UseSqlServer`/`UseNpgsql`. Text
  comparison follows the database's collation: SQL Server's default ignores case, PostgreSQL's doesn't.
- **Two EF providers can't coexist** on one context. A test host points the module contexts at its own database with a
  later `AddModuleDatabase` (`NovaCoreApiFactory`'s SQLite, the PostgreSQL layer, the template's test factory), so
  Program's SQL Server is never configured and every context still goes through `AddModuleDbContext` (see
  `tests/CLAUDE.md`).
- **Suppressed-by-design warnings** (all user-approved, DECISIONS D-011):
  - `Directory.Build.props`: `NoWarn` `RMG020` (Mapperly intentional source-member drops). `WarningsNotAsErrors`
    `NU1901;NU1902` (low/moderate advisories stay warnings; high/critical fail restore; CI fails on any).
  - Allowed by Microsoft's "When to suppress" section (DECISIONS Y-46): `.editorconfig` CA1861 off for
    `{src,tests}/**/Migrations/*.cs` (EF migrations, R-064); `[SuppressMessage]` CA5350 on the test TOTP generator (R-063).
  - `.editorconfig`: CA1707 is off for `tests/**.cs` (test names are sentences).
  - Allowed by Microsoft's "When to suppress" (D-064, D-065): CA2007 off for the host, the modules, the tests and the
    two packages that run only inside ASP.NET Core (`NovaCore.AspNetCore`, `NovaCore.Identity.OpenIddict`); the other
    packages call `ConfigureAwait(false)`. CA1812 off everywhere (DI and framework-created classes). CA1506 with a
    justified `[SuppressMessage]` on three composition roots and protocol flows (the template: its `Program.cs`).
  - Meziantou, user-approved (Q-50): MA0048 off (related types share a file), MA0004 off where CA2007 is, MA0051 off
    for migrations (the PostgreSQL test set's included).
  - `[SuppressMessage]` with justification on `Error` (CA1716) and `IDomainEventHandler<T>` (CA1711).
- **`Directory.Build.targets` is too late for some properties.** The SDK computes `DocumentationFile` (from
  `GenerateDocumentationFile`) and Roslyn `DeterministicSourcePaths` (from `ContinuousIntegrationBuild`) before it is
  imported. Set those in the `.csproj` or `Directory.Build.props` (RESEARCH R-040).
- **XML docs reach the OpenAPI document.** .NET 10's OpenAPI source generator copies XML comments from projects with
  `GenerateDocumentationFile` into schema descriptions. Doc comments on packable types are user-visible API docs (R-041).
- **Keep the `NovaCore.Modules.*` assembly-name prefix** for any new module: modules are registered explicitly, but
  the architecture tests find modules (and check their boundaries) by that prefix.
- **Renaming or moving a project leaves stale DLLs** in `tests/**/bin` and `src/Api/**/bin`, and the architecture
  tests load every `NovaCore.*.dll` there. Delete those `bin`/`obj` folders after a rename; CI starts clean.

---

## Memory & docs

Durable project facts are also recorded in Claude's memory (`novacore-project`, `novacore-stack`,
`novacore-security`). The `README.md` has the full architecture/security write-up with sources; prefer
updating both this file and the README when conventions change.
