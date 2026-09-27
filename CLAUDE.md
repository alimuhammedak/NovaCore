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

- **Phase work starts only via `/novacore-phase <id>`** (project skill, user-invoked), one phase per fresh
  session. When a phase ends, **stop**. "devam"/"continue" means "next step inside the approved phase
  plan", never "start the next phase".
- **Never implement anything in DECISIONS.md → "Yapılmayacaklar"**, not even as a design suggestion. If one
  seems necessary, stop and ask with the reason and a source.
- **Counter the "most typical answer" pull (Y-40…Y-43, applies to every session, not only phase work):**
  every proposed pattern/abstraction/package names the concrete problem it solves *in this codebase*
  (file/line), one simpler alternative and why it falls short, and a primary source. When asked to choose
  anything (design, naming, even a color), offer 2–3 genuinely different options with trade-offs and a
  project-specific recommendation. "Best practice"/"commonly done" is never a reason on its own. Never
  re-propose a rejected approach under a new name; say "I propose reopening Y-xx" instead.
- **Make technical decisions yourself (DECISIONS D-016).** Don't guess: research the primary source
  (Microsoft Learn MCP: `microsoft_docs_search` / `microsoft_docs_fetch` / `microsoft_code_sample_search`;
  SDK files and `dotnet msbuild -pp`). If Microsoft's docs say how to do it, do it that way. If they don't,
  weigh cost (implementation + maintenance + risk) and pick the sensible option. Then log it (RESEARCH R-entry,
  a new DECISIONS D-entry if it lasts, PROGRESS note) and report it; don't ask. Writing plan docs needs no
  permission.
- **Ask (`AskUserQuestion`) only for the user's decisions:** contradicting or reopening a D/Y record, a
  product/ownership choice the PLAN leaves to the user, a new non-Microsoft package, or an outward/irreversible
  action (merge, push, tag, publish, delete).
- **Another phase's topic?** Read that phase in PLAN.md first. Do the proper work now if it's needed now and
  cheaper overall (record a D-entry); otherwise leave the code alone and note it in PROGRESS →
  "Sonraya bırakılanlar". **No temporary code or suppressions** ("remove in phase X"); they get forgotten (Y-45).
- **Hooks are guardrails, not obstacles** (`.claude/settings.json`): new non-allowlisted NuGet packages and edits
  to `PLAN.md`, `DECISIONS.md`, `.claude/settings.json` and `.claude/hooks/` trigger a user approval prompt.
  `DECISIONS.md` prompts even for a new entry; policy-wise new entries are pre-approved (D-016), so batch them
  into one edit per phase step. Banned packages are denied, and the Stop hook blocks finishing while build/tests
  are red. Don't try to route around them. The auto-mode classifier also blocks the model from editing hooks.
- Outside the restructuring (a quick question, a one-off fix the user asks for), work normally, but still
  respect DECISIONS.md.

---

## Commands

```bash
# Build / test
dotnet build                                  # whole solution (NovaCore.slnx); TreatWarningsAsErrors: any warning fails the build
dotnet restore NovaCore.slnx --force          # incremental builds skip restore, so NuGet audit (NU19xx) stays hidden without this
dotnet build -c Release
dotnet test                                   # all tests (no SQL Server needed — every DbContext runs on SQLite)
dotnet test --filter "FullyQualifiedName~AuthorizationCodeFlowTests.Refresh_rotates_the_refresh_token"   # single test

# Run (Development opens Scalar UI at /scalar/v1)
dotnet run --project src/Api/NovaCore.Api

# EF Core migrations — THREE separate DbContexts: one per module, plus the identity provider's in the host. Restore tools first.
dotnet tool restore
dotnet ef migrations add <Name> --project src/Modules/<M>/NovaCore.Modules.<M> --startup-project src/Modules/<M>/NovaCore.Modules.<M> --context <M>DbContext --output-dir Infrastructure/Migrations
dotnet ef database update     --project src/Modules/<M>/NovaCore.Modules.<M> --startup-project src/Modules/<M>/NovaCore.Modules.<M> --context <M>DbContext
#   <M> ∈ { Catalog, Payments }. The module is BOTH -p and -s (each has a design-time factory).
# The identity provider's context comes from the NovaCore.Identity.OpenIddict package; its migrations live in the Api (R-060):
dotnet ef migrations add <Name> --project src/Api/NovaCore.Api --startup-project src/Api/NovaCore.Api --context IdentityServerDbContext --output-dir Identity/Migrations

# Scaffold a fresh project from this repo (it is a `dotnet new` template)
dotnet new install .
dotnet new novacore -n MyProject

# NuGet packages (the seven IsPackable building blocks) → artifacts/ (git-ignored). Version: VersionPrefix in Directory.Build.props.
dotnet pack NovaCore.slnx -c Release -o artifacts [--version-suffix preview.1]
```

There is no separate lint step: every build runs the `latest-recommended` analyzers with `EnforceCodeStyleInBuild=true`
and `TreatWarningsAsErrors=true`. `dotnet format` is available but optional. Publishing is `.github/workflows/release.yml`
on a `v*` tag that must match `VersionPrefix` (GitHub Packages).
The connection string targets a **local SQL Server via Windows auth** (`appsettings.json`).

---

## Architecture (the big picture)

**Hybrid: modular monolith (macro) + vertical slice (micro) + a thin shared core.** Read these together:

- **Packages (`src/BuildingBlocks/`, ANALYSIS §4):** three **core** packages that depend on the BCL and Microsoft
  packages only (D-003) — `NovaCore.Abstractions` (BCL only), `NovaCore.AspNetCore` and `NovaCore.EntityFrameworkCore`
  — plus optional **add-on** packages for third-party libraries: `NovaCore.Serilog`, `NovaCore.OpenTelemetry`,
  `NovaCore.Scalar`, `NovaCore.Identity.OpenIddict` (the OpenID Connect provider).

- **Dependency direction (one way):** `Api` → `Modules.*` → `NovaCore.AspNetCore` / `NovaCore.EntityFrameworkCore`
  → `NovaCore.Abstractions` → BCL. AspNetCore and EntityFrameworkCore never reference each other; add-ons reference
  at most AspNetCore/Abstractions. Enforced by `PackageDependencyTests`.

- **The host composes everything explicitly** (`src/Api/NovaCore.Api/Program.cs`): the add-ons (`AddCoreLogging`,
  `AddCoreObservability`), the core (`AddCoreDefaults`, `AddCoreWeb`, `AddCorePersistence`), the identity provider
  (`AddCoreIdentityServer` + `AddModuleDbContext<IdentityServerDbContext>` with its SQL Server store), then one
  `builder.AddModule<TModule>()` per module; after `Build`,
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

- **Persistence:** each module owns a `DbContext` pinned to its own SQL Server schema
  (`catalog` / `payments` / `identity`, via `HasDefaultSchema`). Register through
  `services.AddModuleDbContext<TContext>(...)` (`NovaCore.EntityFrameworkCore`) — this also enrolls the context for
  generic startup migration (`DatabaseMigrationService`, registered by `AddCorePersistence`; it migrates only when
  `Database:ApplyMigrations` is true).

- **Security** is a first-class subsystem. The core only *validates* access tokens (D-002): JWT bearer against an
  OpenID Connect provider, `Jwt:Authority` (issuer + JWKS discovered, asymmetric algorithms only), required in every
  environment (D-028). Permission-based authorization lives in
  `NovaCore.AspNetCore` too: permissions are resolved on the server through `IPermissionResolver` (never read from the
  token) and cached per `sub` + security stamp for `Cache:PermissionExpiration`. The tokens come from the
  `NovaCore.Identity.OpenIddict` add-on (ASP.NET Core Identity + OpenIddict: Authorization Code + PKCE, refresh
  rotation, client credentials, 2FA, admin), which the Api host runs as its own authority. See
  `src/BuildingBlocks/NovaCore.Identity.OpenIddict/CLAUDE.md` before touching auth.

---

## Standards (do / don't)

These are hard rules. Breaking them defeats the point of the project or fails an architecture test.

- **Packages: MIT/Apache (or Microsoft built-in) ONLY.** **Never add MediatR, AutoMapper, or
  FluentAssertions** — they are now commercial. Use: Mapperly (mapping), .NET 10's built-in validation
  (validation), Scalar (OpenAPI UI). If a mediator is ever truly needed, use MIT `martinothamar/Mediator`.

- **No mediator / no CQRS dispatcher.** Handlers receive dependencies directly from minimal-API DI
  (`sade CQRS`). Don't introduce a request-dispatch abstraction.

- **`NovaCore.Abstractions` stays framework-free** (BCL only — no ASP.NET, no EF), and the core packages depend on
  Microsoft packages only; third-party libraries go into an add-on package. Enforced by `ModularityTests` and
  `PackageDependencyTests`. Put anything needing ASP.NET Core in `NovaCore.AspNetCore` and anything needing EF Core in
  `NovaCore.EntityFrameworkCore`.

- **Modules never reference each other directly.** Cross-module calls go through a `*.Contracts` project
  (in-process interface, e.g. `IProductCatalog`) or a domain event — never another module's DbContext.

- **Expected failures → `Result`/`Result<T>` + `.ToHttpResult()`** (`NovaCore.AspNetCore`). Reserve
  exceptions for the genuinely exceptional. Domain exceptions live in `NovaCore.AspNetCore/Exceptions`.

- **Endpoints are guarded by permissions, not ad-hoc role checks:**
  `endpoint.RequirePermission("module:resource:action")`. Declare a module's permissions via
  `IPermissionDefinitionProvider` + `AddPermissionDefinitions<T>()`. The `Administrator` role is super-admin.

- **Central Package Management:** versions live ONLY in `Directory.Packages.props`. `*.csproj` use
  versionless `<PackageReference>`. Common build props are in `Directory.Build.props` (net10.0, nullable,
  implicit usings, analyzers, version); package metadata for `IsPackable` projects is in `Directory.Build.targets`;
  per-project csproj stay tiny.

- **0 warnings is enforced** (`TreatWarningsAsErrors`). Fix a warning; don't suppress it. Suppressing (`NoWarn`,
  `#pragma`, `.editorconfig` severity, `[SuppressMessage]`) needs user approval (DECISIONS Y-10). The approved ones are
  listed under Gotchas.

- **Configuration goes through validated options, never read at registration.** Bind a section with
  `AddOptions<T>().BindConfiguration(section).ValidateOnStart()`, put the rules on the options type as DataAnnotations
  with an `[OptionsValidator]` partial validator (rules attributes can't express go in `.Validate(...)`), and configure
  framework options that depend on it with `AddOptions<TFramework>().Configure<IOptions<T>>(...)`. No
  `configuration.GetValue`/`Get<T>` while registering services: values added after `CreateBuilder` (tests, hosts)
  would be missed (docs/plan/RESEARCH.md R-048).
- **Time comes from an injected `TimeProvider`**, never `DateTime(Offset).UtcNow`/`Now` (`SourceHygieneTests`
  enforces it under `src/`). The building blocks register `TimeProvider.System` with `TryAddSingleton` next to their
  consumers; tests swap in `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`). The one exception is the
  `DomainEvent.OccurredOnUtc` default (DECISIONS D-021).
- **Logging is source-generated** (CA1848 is an error): `[LoggerMessage]` partial methods, explicit `EventId`s unique
  within the assembly (SYSLIB1006). Default to instance methods on the class's primary-constructor `ILogger`. When an
  argument must be computed (CA1873), wrap the call in an `IsEnabled` guard and declare the methods as `this ILogger`
  extensions: CA1873 only accepts a guard on the same receiver (`SecurityAuditLogger`, RESEARCH R-047).

- **Packable projects are NuGet packages** (the three core packages and the four add-ons in `src/BuildingBlocks`):
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
- **SQL Server, not Postgres:** use `EF.Functions.Like` (not `ILike`); `UseSqlServer`, not `UseNpgsql`.
- **Two EF providers can't coexist** in one container. The test host swaps every DbContext to SQLite and must
  give them their own internal service provider (see `tests/CLAUDE.md`).
- **Suppressed-by-design warnings** (all user-approved, DECISIONS D-011):
  - `Directory.Build.props`: `NoWarn` `RMG020` (Mapperly intentional source-member drops). `WarningsNotAsErrors`
    `NU1901;NU1902` (low/moderate advisories stay warnings; high/critical fail restore; CI fails on any).
  - Allowed by Microsoft's "When to suppress" section (DECISIONS Y-44): `.editorconfig` CA1861 off for
    `src/**/Migrations/*.cs` (EF migrations, R-064); `[SuppressMessage]` CA5350 on the test TOTP generator (R-063).
  - `.editorconfig`: CA1707 is off for `tests/**.cs` (test names are sentences).
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
