# CLAUDE.md — Modules (vertical-slice business modules)

Subtree rules for `src/Modules/`. See the repo-root `CLAUDE.md` for global standards.

## Adding a module (the only supported workflow)

1. `dotnet new classlib -o src/Modules/<M>/NovaCore.Modules.<M>` — the assembly name **must** start with
   `NovaCore.Modules.` (that prefix is how `ModuleLoader` discovers it).
2. Reference `NovaCore.Core.Web`. Add packages: `Microsoft.EntityFrameworkCore.SqlServer`,
   `Microsoft.EntityFrameworkCore.Design`, `Riok.Mapperly`, `FluentValidation(.DependencyInjectionExtensions)`.
3. Implement `IModule` (`<M>Module.cs`): `AddModule` registers the DbContext + permissions + validators;
   `MapEndpoints` maps a `MapGroup("/api/v1/<area>")`.
4. Add a `ProjectReference` from `src/Api/NovaCore.Api`. **Done — no central wiring file changes.**

Use `Catalog` as the reference implementation (full CRUD + contracts); `Payments` shows cross-module use.

## Anatomy of a module

- **`Features/`** — one file per feature: `record Request`, `record Response`, `AbstractValidator`,
  `Map(group)`, and a static `Handle` that takes its dependencies as minimal-API parameters
  (DbContext, contracts, `CancellationToken`). No mediator.
- **`Domain/`** — entities derive from `AuditableEntity<Guid>` (Guid v7 keys — friendly to SQL Server
  clustering). Keep setters private; mutate through methods.
- **`Infrastructure/`** — `<M>DbContext` (`public const string Schema`, `HasDefaultSchema(Schema)`,
  `ApplyConfigurationsFromAssembly`), `IEntityTypeConfiguration<>` per entity, and a
  `IDesignTimeDbContextFactory<>` (so `dotnet ef` works offline with the module as `-p` and `-s`).
- **`Contracts/`** — for a module others consume, expose a **separate `*.Contracts` project**
  (interfaces + DTOs only). Consumers reference the Contracts project, **never** the module.
- **`Authorization/`** — `<M>Permissions` constants (`module:resource:action`) + an
  `IPermissionDefinitionProvider`; register with `AddPermissionDefinitions<...>()` in the module.
- **`Mapping/`** — Mapperly static partial mappers; add a `ProjectToDto` IQueryable projection for lists.

## Rules specific to modules

- **DbContext registration:** `services.AddModuleDbContext<<M>DbContext>((sp, o) => { o.UseSqlServer(cfg
  .GetConnectionString("Database"), sql => { sql.MigrationsHistoryTable("__EFMigrationsHistory", Schema);
  sql.EnableRetryOnFailure(); }); o.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()); });`
- **Guard every endpoint** with `.RequirePermission(<M>Permissions.X)` (or `.RequireAuthorization()` only
  when no finer permission applies). Add the validation filter for write DTOs:
  `.AddEndpointFilter<ValidationFilter<Request>>()`.
- **Don't reach across modules' data.** Need another module's data? Call its `*.Contracts` interface
  (resolved via DI at runtime) — like `Payments` calling `IProductCatalog`.
- **`WithTags` on the group** requires `using Microsoft.AspNetCore.Http;`.
- After adding/altering an entity, generate a migration for that module's context (see root `CLAUDE.md`).
