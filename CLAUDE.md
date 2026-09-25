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
- **Something not in the plan?** Don't guess. Research the primary source (Microsoft Learn MCP:
  `microsoft_docs_search` / `microsoft_docs_fetch` / `microsoft_code_sample_search`), log it in
  `docs/plan/RESEARCH.md`, and if it affects scope, public API, dependencies or a recorded decision, ask
  the user with `AskUserQuestion`. Out-of-scope findings go to PROGRESS.md → "Sonraya bırakılanlar".
- **Hooks are guardrails, not obstacles** (`.claude/settings.json`): new NuGet packages and edits to
  protected files (`PLAN.md`, `DECISIONS.md`, `.claude/settings.json`, `.claude/hooks/`) require user
  approval; banned packages are denied; the Stop hook blocks finishing while build/tests are red. Don't
  try to route around them.
- Outside the restructuring (a quick question, a one-off fix the user asks for), work normally, but still
  respect DECISIONS.md.

---

## Commands

```bash
# Build / test
dotnet build                                  # whole solution (NovaCore.slnx); 0 warnings is the bar, but see below
dotnet restore NovaCore.slnx --force          # incremental builds skip restore, so NuGet audit (NU19xx) stays hidden without this
dotnet build -c Release
dotnet test                                   # all tests (no SQL Server needed — Identity runs on SQLite)
dotnet test --filter "FullyQualifiedName~AuthFlowTests.Refresh_rotates_the_refresh_token"   # single test

# Run (Development opens Scalar UI at /scalar/v1)
dotnet run --project src/Api/NovaCore.Api

# EF Core migrations — THREE separate DbContexts, one per module. Restore tools first.
dotnet tool restore
dotnet ef migrations add <Name> --project src/Modules/<M>/NovaCore.Modules.<M> --startup-project src/Modules/<M>/NovaCore.Modules.<M> --context <M>DbContext --output-dir Infrastructure/Migrations
dotnet ef database update     --project src/Modules/<M>/NovaCore.Modules.<M> --startup-project src/Modules/<M>/NovaCore.Modules.<M> --context <M>DbContext
#   <M> ∈ { Catalog, Payments, Identity }. The module is BOTH -p and -s (each has a design-time factory).

# Scaffold a fresh project from this repo (it is a `dotnet new` template)
dotnet new install .
dotnet new novacore -n MyProject
```

There is no separate lint step (`EnforceCodeStyleInBuild=false`); `dotnet format` is available but optional.
The connection string targets a **local SQL Server via Windows auth** (`appsettings.json`).

---

## Architecture (the big picture)

**Hybrid: modular monolith (macro) + vertical slice (micro) + a thin shared core.** Read these together:

- **Dependency direction (one way):** `Api` → `Modules.*` → `Core.Web` → `Core.Infrastructure` →
  `Core.Abstractions` → BCL. `Api` is a ~15-line composition root (`src/Api/NovaCore.Api/Program.cs`):
  `AddCoreDefaults()` + `AddCoreWeb()` + `AddModules()`, then `UseCoreWeb()` + `MapModules()` + `MapDefaultEndpoints()`.

- **Modules are auto-discovered, not centrally registered.** `ModuleLoader` (Core.Web) scans deploy-dir
  assemblies named `NovaCore.Modules.*` for `IModule` implementations. Each `IModule` registers its own
  services + DbContext + permissions and maps its own endpoints. **Adding a module = new project +
  implement `IModule` + reference it from `Api`. No other file changes.** (See `src/Modules/CLAUDE.md`.)

- **Cross-cutting lives in the core, owned by `Core.Web`:** `AddCoreDefaults` (Serilog + OpenTelemetry +
  health + HttpClient resilience), `AddCoreWeb`/`UseCoreWeb` (ProblemDetails + exception handlers,
  OpenAPI + Scalar, JWT auth, permission authz, rate limiter, CORS, security headers).

- **Persistence:** each module owns a `DbContext` pinned to its own SQL Server schema
  (`catalog` / `payments` / `identity`, via `HasDefaultSchema`). Register through
  `services.AddModuleDbContext<TContext>(...)` (Core.Infrastructure) — this also enrolls the context for
  generic startup migration (`DatabaseMigrationService`, gated by `Database:ApplyMigrations`).

- **Security** is a first-class subsystem: the `Identity` module (ASP.NET Core Identity + JWT/refresh +
  2FA + admin) plus permission-based-authorization infrastructure in `Core.Web`. See
  `src/Modules/NovaCore.Modules.Identity/CLAUDE.md` before touching auth.

---

## Standards (do / don't)

These are hard rules. Breaking them defeats the point of the project or fails an architecture test.

- **Packages: MIT/Apache (or Microsoft built-in) ONLY.** **Never add MediatR, AutoMapper, or
  FluentAssertions** — they are now commercial. Use: Mapperly (mapping), FluentValidation (validation),
  Scalar (OpenAPI UI). If a mediator is ever truly needed, use MIT `martinothamar/Mediator`.

- **No mediator / no CQRS dispatcher.** Handlers receive dependencies directly from minimal-API DI
  (`sade CQRS`). Don't introduce a request-dispatch abstraction.

- **`Core.Abstractions` stays framework-free** (BCL only — no ASP.NET, no EF). Enforced by
  `ModularityTests`. Put anything needing the framework in `Core.Infrastructure`/`Core.Web`.

- **Modules never reference each other directly.** Cross-module calls go through a `*.Contracts` project
  (in-process interface, e.g. `IProductCatalog`) or a domain event — never another module's DbContext.

- **Expected failures → `Result`/`Result<T>` + `.ToHttpResult()`** (Core.Infrastructure). Reserve
  exceptions for the genuinely exceptional. Domain exceptions live in `Core.Infrastructure/Exceptions`.

- **Endpoints are guarded by permissions, not ad-hoc role checks:**
  `endpoint.RequirePermission("module:resource:action")`. Declare a module's permissions via
  `IPermissionDefinitionProvider` + `AddPermissionDefinitions<T>()`. The `Administrator` role is super-admin.

- **Central Package Management:** versions live ONLY in `Directory.Packages.props`. `*.csproj` use
  versionless `<PackageReference>`. Common build props are in `Directory.Build.props` (net10.0, nullable,
  implicit usings); per-project csproj stay tiny.

- **Vertical slice:** one feature = one file (request + response + validator + route + handler together)
  under a module's `Features/`. Don't spread a feature across layered folders.

- **Mapping:** Mapperly static partial mappers (`[Mapper]`). For EF list queries use a source-generated
  `ProjectToDto` (IQueryable projection) so it translates to SQL.

---

## Gotchas (learned the hard way)

- **Eager config reads.** `AddCoreWeb` reads `RateLimiting:PermitLimit`, `Jwt:SigningKey`, and
  `Database:ApplyMigrations` *eagerly* during service registration. `WebApplicationFactory`'s in-memory
  config is layered in too late to override these — set them as **environment variables** in tests, or
  per factory via `CreateHost` + `ConfigureHostConfiguration` (see `tests/CLAUDE.md`).
- **`WithTags` on a route group** needs `using Microsoft.AspNetCore.Http;` (not just `...Builder`).
- **SQL Server, not Postgres:** use `EF.Functions.Like` (not `ILike`); `UseSqlServer`, not `UseNpgsql`.
- **Two EF providers can't coexist** in one container. The test host swaps Identity to SQLite and must
  give it its own internal service provider (see `tests/CLAUDE.md`).
- **Suppressed-by-design warnings** (`Directory.Build.props`): `EXTEXP0018` (HybridCache is GA but flagged
  experimental), `RMG020` (Mapperly intentional source-member drops), `CS1591` (XML docs off).
- **Module discovery** is reflection-based (incompatible with NativeAOT) and keys off the assembly name
  prefix `NovaCore.Modules.*` — keep that prefix for any new module.

---

## Memory & docs

Durable project facts are also recorded in Claude's memory (`novacore-project`, `novacore-stack`,
`novacore-security`). The `README.md` has the full architecture/security write-up with sources; prefer
updating both this file and the README when conventions change.
