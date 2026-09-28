# CLAUDE.md — Modules (vertical-slice business modules)

Subtree rules for `src/Modules/`. See the repo-root `CLAUDE.md` for global standards.

## Adding a module (the only supported workflow)

1. `dotnet new classlib -o src/Modules/<M>/NovaCore.Modules.<M>` — the assembly name **must** start with
   `NovaCore.Modules.` (the architecture tests find modules by that prefix).
2. Reference `NovaCore.AspNetCore` and `NovaCore.EntityFrameworkCore`. Add packages:
   `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `Riok.Mapperly`.
   Validation needs no package: the ASP.NET Core framework (via `NovaCore.AspNetCore`) brings it.
3. Implement `IModule` (`<M>Module.cs`): `AddModule` registers the DbContext + permissions and calls
   `services.AddValidation()`; `MapEndpoints` maps a `MapGroup("/api/v1/<area>")`.
4. Add a `ProjectReference` from `src/Api/NovaCore.Api` and one line to its `Program.cs`:
   `builder.AddModule<<M>Module>();` (modules map their endpoints in the order they are added).

Use `Catalog` as the reference implementation (full CRUD + contracts); `Payments` shows cross-module use. Together
they show a cross-module write through events both ways (DECISIONS D-034): a payment is saved pending with
`PaymentCreated` (`Payments.Contracts`), Catalog's `ReserveStockForPayment` takes the stock or refuses and answers with
`StockReserved`/`StockReservationFailed` (`Catalog.Contracts`), and Payments' `SettlePayment` completes or fails it.

## Anatomy of a module

- **`Features/`** — one file per feature: `record Request` (validation attributes on its parameters),
  `record Response`, `Map(group)`, and a static `Handle` that takes its dependencies as minimal-API parameters
  (DbContext, contracts, `CancellationToken`). No mediator.
- **`Domain/`** — entities derive from `AuditableEntity<Guid>` (Guid v7 keys — friendly to SQL Server
  clustering). Keep setters private; mutate through methods.
- **`Infrastructure/`** — `<M>DbContext` (`public const string Schema`, `HasDefaultSchema(Schema)`,
  `ApplyConfigurationsFromAssembly`, `modelBuilder.AddOutbox()`), `IEntityTypeConfiguration<>` per entity (each
  ignores `DomainEvents`), and a `IDesignTimeDbContextFactory<>` (so `dotnet ef` works offline with the module as `-p`
  and `-s`).
- **`Contracts/`** — for a module others consume, expose a **separate `*.Contracts` project**
  (interfaces, DTOs and the events it publishes; `NovaCore.Abstractions` only, for `DomainEvent`). Consumers reference the
  Contracts project, **never** the module.
- **`Authorization/`** — `<M>Permissions` constants (`module:resource:action`) + an
  `IPermissionDefinitionProvider`; register with `AddPermissionDefinitions<...>()` in the module.
- **`Mapping/`** — Mapperly static partial mappers; add a `ProjectToDto` IQueryable projection for lists.

## Rules specific to modules

- **DbContext registration:** `services.AddModuleDbContext<<M>DbContext>((sp, o) => { o.UseSqlServer(cfg
  .GetConnectionString("Database"), sql => { sql.MigrationsHistoryTable("__EFMigrationsHistory", Schema);
  sql.EnableRetryOnFailure(); }); o.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()); });`
  `AddModuleDbContext` adds the `OutboxInterceptor` itself; the context maps the table with `modelBuilder.AddOutbox()`.
- **Domain events:** an entity records one with `Raise(new SomethingHappened(...))` (a record deriving from
  `DomainEvent`). Saving the entity writes the event to the module's `outbox_messages` table in the same transaction.
  `ModuleRegistrationTests` checks that every module context maps the outbox.
  - The outbox dispatcher delivers it. Register a handler explicitly in `AddModule`:
    `services.AddScoped<IDomainEventHandler<SomethingHappened>, DoSomething>()`.
  - Each handler runs **at least once**, so make it idempotent. The handlers of one event share a DI scope (and its
    DbContext instances), as in a request, so each saves what it changes.
  - A handler that changes state keeps a record keyed by the message's business id, saves it in the same `SaveChanges`
    as the change, and returns early when it finds it (`StockReservation`, keyed by the payment). The dispatcher's
    `outbox_consumers` record is written after the handler's own save, so it can't stop a second run after a crash.
  - An event another module handles lives in the publisher's `*.Contracts` project, because modules reference each
    other only through Contracts.
- **Guard every endpoint** with `.RequirePermission(<M>Permissions.X)` (or `.RequireAuthorization()` only
  when no finer permission applies).
- **Validate write DTOs with DataAnnotations** on the request record (`[Required, MaxLength(200)] string Name`).
  The module's `services.AddValidation()` call makes .NET 10 run them before the handler and answer 400 with a
  validation ProblemDetails. Without that call nothing is validated, and nothing fails either. A rule no built-in
  attribute expresses: a custom `ValidationAttribute` for one value (`CreatePayment`'s `NotEmpty`), `IValidatableObject`
  for several properties. Add the endpoint to `ValidationTests`.
- **Don't reach across modules' data.** Need another module's data? Call its `*.Contracts` interface
  (resolved via DI at runtime) — like `Payments` calling `IProductCatalog`.
- **`WithTags` on the group** requires `using Microsoft.AspNetCore.Http;`.
- After adding/altering an entity, generate a migration for that module's context (see root `CLAUDE.md`).
