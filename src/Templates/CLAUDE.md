# CLAUDE.md — Templates (the `dotnet new novacore` template package)

Subtree rules for `src/Templates/`. See the repo-root `CLAUDE.md` for global standards and `docs/plan/DECISIONS.md`
D-040/D-041 for why it is built this way (RESEARCH R-090…R-092).

## Shape

- `NovaCore.Templates/NovaCore.Templates.csproj` is a template package (`PackageType=Template`, package ID
  `BMD.NovaCore.Templates`). It compiles nothing: `content/novacore/**` is packed as content, everything at
  `content/novacore/...` in the package. Symbols, package validation, XML docs and PublicAPI tracking don't apply to it
  (`NovaCoreLibraryPackage` in `Directory.Build.targets`); NU5128 is avoided with `SuppressDependenciesWhenPacking`.
- `content/novacore` is the project `dotnet new novacore` writes, with `sourceName` **`MyService`** (every form of it is
  replaced: `MyService`, `myservice`). Never use `MyService` for anything that must survive the rename, and never write
  `NovaCore` where the user's name belongs.
- **The NovaCore version is stamped when packing:** the template's `Directory.Packages.props` says
  `<NovaCoreVersion>NOVACORE_PACKAGE_VERSION</NovaCoreVersion>`; the `NovaCoreStampPackageVersion` target
  (`TargetsForTfmSpecificContentInPackage`) packs a copy with `$(PackageVersion)`. Package references use the
  `BMD.NovaCore.*` IDs; `using`s stay `NovaCore.*`.
- **`--auth BuiltIn|External`** (`BuiltInAuth` computed symbol), **`--db SqlServer|PostgreSql`** (`DbSqlServer`,
  `DbPostgreSql`) and **`--multitenancy`** (bool, default off, used directly: `multitenancy`; D-076 (12), D-084). Whole
  files that only one option has are excluded in `template.json` → `sources.modifiers`; differences inside a file use
  the template engine's conditionals: `#if (...)` in `.cs`, `.yml` and `.sh`, `<!--#if (...) -->` in MSBuild files and
  `nuget.config`, `//#if (...)` in `.json`.
- **`--multitenancy`** adds the `NovaCore.MultiTenancy` add-on to the Api (`AddCoreMultiTenancy`, the tenant store's
  context and migrations, `MultiTenancy:Roles`) and makes `Todo` a tenant entity under
  `/api/v1/tenants/{tenant}/todos` (`RequireTenant()`); the module still doesn't reference the add-on. Its tests are
  `TenantTodosTests.cs` in place of `TodosTests.cs`.
- **The database is the Api's alone** (D-073, D-075): the provider package, `AddModuleDatabase` in `Program.cs`,
  `DesignTimeDatabase.cs`, the connection string, `eng/check-licenses.sh`'s named exception and each context's migrations.
  The Todos module and the tests don't change with `--db`.
- Ports come from the `port` generator: `NOVACORE_HTTPS_PORT` and `NOVACORE_HTTP_PORT` in the content are its
  placeholders. Placeholders are never bare numbers: the engine replaces every occurrence, a migration's timestamp too.

## Rules for the content

- It is a consumer's project, built with warnings as errors, analyzers and NuGet audit on. It follows the repo's
  standards (vertical slices, typed results, permissions, validated options, `TimeProvider`) because it teaches them.
  Its analyzers are the repo's built-in ones (D-065 (4)): the four `AnalysisLevel<Category>=latest-all` categories, the
  dead-code IDE rules and the code metrics; CA2007 and CA1812 are off for the whole solution (application code) and
  CA1506 for its `Program.cs`. Not BannedApi or Meziantou (repository-only), and no format check in its CI: its usings
  sort by the project's name, so a generated project isn't format-clean for every name.
- It is under `src/`, so the architecture tests scan its `.cs` files too (TODO/FIXME, ambient clocks, assembly
  scanning). Fix the code, don't exempt it.
- Nothing of this repository goes into it: no BuildingBlocks source, `docs/plan`, `.claude/`, `release.yml`, PublicAPI
  files, `CLAUDE.md`, author name. The smoke test checks the obvious ones.
- **Migrations:** the `Initial` migrations (Todos, the identity store for BuiltIn, the tenant store for
  `--multitenancy`) are generated, never hand-written, one set per provider and, for Todos, per `--multitenancy`. SQL
  Server's are in `src/Api/MyService.Api/{Todos,Identity,MultiTenancy}/Migrations`, PostgreSQL's in
  `.../MigrationsPostgreSql` (same file names); a tenant's Todos are in `Todos/SqlServerTenantMigrations` and
  `Todos/PostgreSqlTenantMigrations`. `sources.modifiers` drops the folders the options don't choose, and a constant
  symbol's `fileRename` per folder name turns the chosen one into `Migrations`. `fileRename` replaces every occurrence
  in a path, so no folder token may contain another (`MigrationsPostgreSql` is why the tenant ones start with the
  provider). After a model change: pack, create a project named `MyService` from the package with each `--db` (and
  `--multitenancy`), run `dotnet ef migrations add` there (`--project` and `--startup-project` the Api), copy the files
  back into the option's folder. A NovaCore model change reaches these too (the outbox's columns are in every module's
  set).
- A package the content references must be in `content/novacore/Directory.Packages.props`; a new non-Microsoft one
  needs the user's approval like anywhere else.

## Proving a change

```bash
bash eng/template-smoke.sh [work-dir]
```

It packs every package into a local feed (`--version-suffix smoke`), installs the template into a template hive of its
own (`--debug:custom-hive`), creates a project for each `--auth`, `--db` and `--multitenancy` (eight), points its
`novacore` source at the feed, restores into a package cache of its own, then builds (warnings are errors), tests, runs
the project's license gate and `has-pending-model-changes` for each context, and checks that no repository file,
unreplaced token or `#if` got in, that only the chosen database is there, and that multi-tenancy is there only with
`--multitenancy`. On Windows run it without a work directory (it takes a short
temporary one): a deep path pushes a native library past MAX_PATH. CI runs the same script
(`template-smoke` job), which then lints the generated projects' `.github` with `eng/lint-workflows.sh` (actionlint,
zizmor). The content's workflow pins its actions to the same commit SHAs as the repository's workflows
(`WorkflowPinTests`): when Dependabot updates the repository's, update `content/novacore/.github/workflows/ci.yml` too.
The content's `.github/dependabot.yml` keeps a generated project's pins up to date. The generated project's own CI
isn't run by the smoke test: after changing that workflow, run its steps in a generated project.
