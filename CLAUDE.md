# CLAUDE.md

NovaCore is a **.NET 10 single-developer modular-monolith Web API** starter. This file is the operating manual: the
plan protocol, the commands and the **non-negotiable standards**. It stays short on purpose; the detail lives next to
the code and loads when you work there:

| Working on | Read |
|---|---|
| a package under `src/BuildingBlocks` | `src/BuildingBlocks/CLAUDE.md` |
| sign-in, tokens, accounts | `src/BuildingBlocks/NovaCore.Identity.OpenIddict/CLAUDE.md` |
| a module, an endpoint, a feature | `src/Modules/CLAUDE.md` |
| the `dotnet new` template | `src/Templates/CLAUDE.md` |
| tests, test hosts, database layers | `tests/CLAUDE.md` |
| design rationale, security, multi-tenancy, the consumer guide | `README.md` |

## Plan protocol (read first)

The restructuring's source of truth is `docs/plan/` (Turkish). **Precedence:** `DECISIONS.md` > `PLAN.md` >
`PROGRESS.md` > this file > the conversation.

- **`PROGRESS.md` holds live state only** (D-097) and stays under 25 KB. Closed records move to `PROGRESS-ARSIV.md`
  the moment they close; deferred work is in `SONRAYA-BIRAKILANLAR.md`. Neither is read at session start: `grep` them.
- **Phase work starts only via `/novacore-phase <id>`** (user-invoked), one phase per session, at most three committed
  steps per session (D-082); then `/clear` and the same command. When a phase ends, **stop**: "devam"/"continue" means
  the next step of the approved phase, never the next phase.
- **The writing session** runs only the build, the step's new or changed test classes (red, then green) and
  `dotnet format`. **`/novacore-verify <id>`** runs once, after the last step, in a fresh session (D-088, D-093): the
  full suite and `code-review`, findings into PROGRESS, no code fixes; then the CodeRabbit CLI, the `faz/...` → `main`
  pull request (CodeRabbit is advisory, D-067) and the merge question. `/novacore-phase` fixes the findings.
- **`/novacore-night`** (user-invoked, D-096) is the one exception: it runs the remaining queue unattended through
  subagents, leaves the full suite to CI, merges a phase's pull request when its conditions hold and never asks.
  What it may not do is listed in its skill; such work is recorded for the user, not done.
- **Never implement anything in DECISIONS → "Yapılmayacaklar"**, not even as a design suggestion. If one seems
  necessary, stop and ask with the reason and a source.
- **Counter the "most typical answer" pull (Y-40…Y-43, every session):** a proposed pattern, abstraction or package
  names the concrete problem it solves in this codebase (file/line), one simpler alternative and why it falls short,
  and a primary source. Asked to choose anything, offer 2–3 genuinely different options with trade-offs and a
  recommendation. "Best practice" is never a reason. Never re-propose a rejected approach under a new name.
- **Make technical decisions yourself (D-016), from the primary source (D-079):** Microsoft technology → Microsoft
  Learn MCP, SDK files, `dotnet msbuild -pp`. Third-party package → find it with Context7, cite the package's own
  repo or docs. Versions and advisories → NuGet MCP's read-only tools. If Microsoft's docs say how, do it that way;
  otherwise weigh cost (implementation + maintenance + risk). Log it (RESEARCH R-entry, a DECISIONS D-entry if it
  lasts, a PROGRESS note) and report it; don't ask.
- **Ask (`AskUserQuestion`) only for the user's decisions:** contradicting or reopening a D/Y record, a product or
  ownership choice the PLAN leaves to the user, a new non-Microsoft package, an outward or irreversible action
  (merge, push, tag, publish, delete).
- **Another phase's topic?** Read that phase first. Do the proper work now if it is needed now and cheaper overall
  (D-entry); otherwise leave the code alone and note it in `SONRAYA-BIRAKILANLAR.md`. **No temporary code or
  suppressions** (Y-45).
- **Hooks are guardrails** (`.claude/settings.json`): a new non-allowlisted NuGet package, an edit to `PLAN.md`,
  `DECISIONS.md`, `.claude/settings.json` or `.claude/hooks/`, an unpinned workflow `uses:` and a secret-looking value
  prompt the user; banned packages are denied; the Stop hook blocks finishing while the build is red. Batch a step's
  D-entries into one edit. Don't route around a hook.
- Outside the restructuring (a question, a one-off fix), work normally and still respect DECISIONS.

**Working habits (D-048 (4), D-079):**
- Find a symbol's callers with the C# LSP; keep grep for text, configuration and the uncompiled template content. Open
  the matching .NET skill as a checklist; DECISIONS, PLAN and this file win over a skill. The `dotnet-test` agents
  only run self-contained audits (test gaps, test quality), never phase code.
- Before an edit, list every place it must reach: callers, tests and fixtures, configuration, the docs that describe
  it, and the template's copies under `src/Templates/.../content` (compiled only by `bash eng/template-smoke.sh`).
- A bug fix starts with the callers: fix the root cause once, in the shared code.
- End a report with what you skipped or did not check.

## Commands

```bash
dotnet build                                   # NovaCore.slnx; any warning fails the build
dotnet format NovaCore.slnx                    # CI runs it with --verify-no-changes
dotnet test                                    # everything; the SQL Server and PostgreSQL layers need Docker (skipped without)
dotnet test --project tests/NovaCore.Api.Tests -- --filter-class "*.AuthorizationCodeFlowTests"   # or --filter-method
dotnet restore NovaCore.slnx --force           # shows NuGet audit (NU19xx); --force-evaluate rewrites the lock files
dotnet restore NovaCore.slnx --locked-mode     # what CI runs
dotnet run --project src/Api/NovaCore.Api      # Development: Scalar UI at /scalar/v1
bash eng/template-smoke.sh [work-dir]          # packs, then builds and tests every template combination
dotnet pack NovaCore.slnx -c Release -o artifacts
```

- **Test filters need `--project`** (solution-wide, a project without a match exits 8, D-063). `dotnet test` runs on
  Microsoft.Testing.Platform: the solution goes through `--solution`, xUnit options after `--`, no `-nologo`.
- **Gates in `eng/`:** `check-licenses.sh`, `check-coverage.sh [Debug] [--update]`, `check-openapi.sh [base]`,
  `mutation.sh`, `lint-workflows.sh`, `find-unused.sh`. CI runs them; `coderabbit review --agent --base main` is the
  phase-end CLI review.
- **Migrations:** four contexts; the Api host is both `--project` and `--startup-project` (`dotnet tool restore`
  first):
  `dotnet ef migrations add <Name> --project src/Api/NovaCore.Api --startup-project src/Api/NovaCore.Api --context <C>DbContext --output-dir <F>/Migrations`
  with `<C>/<F>` ∈ Catalog/Catalog, Payments/Payments, IdentityServer/Identity, MultiTenancy/MultiTenancy. A model
  change needs a migration in each provider's set (D-074): the PostgreSQL set lives in `tests/NovaCore.Api.Tests`
  (`--output-dir PostgreSql/<F>/Migrations`), and the template has its own sets (`src/Templates/CLAUDE.md`).
- **Publishing** is `release.yml` on a `v*` tag that matches `VersionPrefix`; the user's decision, never part of a
  phase. The SDK is pinned exactly (`global.json`, `rollForward: disable`).

## Architecture in brief

Modular monolith (macro) + vertical slices (micro) + a thin shared core. README has the full write-up.

- **Packages** (`src/BuildingBlocks`, IDs `BMD.NovaCore.*`, names and namespaces `NovaCore.*`): three core packages on
  the BCL and Microsoft packages only (`NovaCore.Abstractions`, BCL only; `NovaCore.AspNetCore`;
  `NovaCore.EntityFrameworkCore`) and five add-ons for third-party libraries (`Serilog`, `OpenTelemetry`, `Scalar`,
  `Identity.OpenIddict`, `MultiTenancy`).
- **Dependencies run one way:** `Api` → `Modules.*` → `AspNetCore` / `EntityFrameworkCore` → `Abstractions`. The two
  never reference each other; an add-on references core packages only (`PackageDependencyTests`).
- **The host composes everything explicitly** in `src/Api/NovaCore.Api/Program.cs`; nothing is found by assembly
  scanning (Y-20). A module is a project implementing `IModule`, referenced by the Api, with one
  `builder.AddModule<TModule>()` line; keep the `NovaCore.Modules.*` assembly prefix (the architecture tests find
  modules by it).
- **Persistence:** each module owns a `DbContext` in its own schema, registered with `AddModuleDbContext`. No module
  chooses a database: the host's `AddModuleDatabase` does, and the host keeps the provider's migrations (D-068, D-073).
- **Domain events** go through the transactional outbox only; delivery is at least once, so handlers are idempotent.
- **Security:** the core only validates access tokens (`Jwt:Authority`); permissions are resolved on the server, never
  read from the token. The tokens come from the `NovaCore.Identity.OpenIddict` add-on.
- **Multi-tenancy:** one database, rows carry a `TenantId`; the core isolates every `ITenantEntity`, the optional
  add-on resolves the tenant from the route and the caller's membership.

## Standards

Breaking one defeats the point of the project or fails an architecture test.

- **Packages are MIT/Apache or Microsoft only**, plus the exceptions DECISIONS names (Y-47). **Never MediatR,
  AutoMapper or FluentAssertions**, and no other mediator or request dispatcher (Y-01): handlers take their
  dependencies from minimal-API DI. Mapping is Mapperly, validation is .NET 10's built-in, the OpenAPI UI is Scalar.
- **`NovaCore.Abstractions` stays BCL-only; core packages depend on Microsoft packages only.** Third-party libraries
  go into an add-on.
- **Modules never reference each other.** Cross-module calls go through a `*.Contracts` project or a domain event.
- **Expected failures are `Result`/`Result<T>` + `.ToHttpResult()`**; exceptions are for the exceptional. Handlers
  return typed results (`Results<Ok<T>, ProblemHttpResult>`, never a bare `IResult`); declare problem statuses with
  `.ProducesProblem(...)`.
- **Endpoints are guarded by permissions:** `RequirePermission("module:resource:action")`. Authorization is closed by
  default; a public endpoint says `.AllowAnonymous()`. No token role bypasses a permission check.
- **One feature = one file** under a module's `Features/`. Request validation is DataAnnotations on the request record,
  and every module calls `services.AddValidation()` (a missing call fails silently). Mapping for EF list queries is a
  source-generated `ProjectToDto`.
- **Configuration goes through validated options**
  (`AddOptions<T>().BindConfiguration(...).ValidateOnStart()` + `[OptionsValidator]`), never `GetValue`/`Get<T>` while
  registering services (R-048).
- **Time comes from an injected `TimeProvider`**, never `DateTime(Offset).UtcNow`/`Now`.
- **Logging is source-generated** (CA1848): `[LoggerMessage]` partial methods with explicit `EventId`s unique in the
  assembly. When an argument must be computed (CA1873), guard with `IsEnabled` and declare the methods as
  `this ILogger` extensions (R-047).
- **0 warnings.** Fix a warning; suppressing one needs the user's approval or Microsoft's "When to suppress" for that
  rule (Y-46). The approved suppressions are in `.editorconfig`, `Directory.Build.props` and DECISIONS (D-011, D-064,
  D-065, Q-50); don't add to them. Low and moderate NuGet advisories (NU1901/NU1902) stay warnings locally; high and
  critical ones fail the restore, and CI fails on any.
- **Central Package Management:** versions only in `Directory.Packages.props`. Non-packable projects commit a
  `packages.lock.json`; after a version change run `dotnet restore NovaCore.slnx --force-evaluate` and commit them.
- **Packable projects are public API:** every public member has an XML doc comment (CS1591), a public API change
  updates `PublicAPI.Unshipped.txt` (RS0016/RS0017), and types stay `internal` unless consumers need them (Y-23).
- **GitHub Actions are pinned to a full commit SHA** with the release in a comment; workflows grant `permissions: {}`
  and check out with `persist-credentials: false`. The template's workflow pins the same SHAs (`WorkflowPinTests`).

## Gotchas

- **Provider-neutral code:** modules and packages use EF Core's neutral API (`EF.Functions.Like`); the host's
  `AddModuleDatabase` is the only `UseSqlServer`/`UseNpgsql`. Text comparison follows the database's collation.
- **Two EF providers can't coexist on one context:** a test host replaces the database with a later
  `AddModuleDatabase` (`tests/CLAUDE.md`).
- **`Directory.Build.targets` is too late** for `GenerateDocumentationFile` and `ContinuousIntegrationBuild`: set them
  in the `.csproj` or `Directory.Build.props` (R-040).
- **XML docs on packable types reach the OpenAPI document** as schema descriptions (R-041).
- **Renaming or moving a project leaves stale DLLs** in `tests/**/bin` and `src/Api/**/bin`, and the architecture
  tests load every `NovaCore.*.dll` there. Delete those `bin`/`obj` folders after a rename.

When a convention changes, update this file, the scoped CLAUDE.md it belongs to and the README together.
