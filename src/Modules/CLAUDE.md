# CLAUDE.md — Modules (vertical-slice business modules)

Subtree rules for `src/Modules/`. See the repo-root `CLAUDE.md` for global standards.

## Adding a module (the only supported workflow)

1. `dotnet new classlib -o src/Modules/<M>/NovaCore.Modules.<M>` — the assembly name **must** start with
   `NovaCore.Modules.` (the architecture tests find modules by that prefix).
2. Reference `NovaCore.AspNetCore` and `NovaCore.EntityFrameworkCore`. Add `Riok.Mapperly`. **No database provider**
   (and no `Microsoft.EntityFrameworkCore.Design`): the host chooses the database (`ModuleProviderTests`, D-073).
   Validation needs no package: the ASP.NET Core framework (via `NovaCore.AspNetCore`) brings it.
3. Implement `IModule` (`<M>Module.cs`): `AddModule` registers the DbContext + permissions and calls
   `services.AddValidation()`; `MapEndpoints` maps a `MapGroup("/api/v1/<area>")`.
4. Add a `ProjectReference` from `src/Api/NovaCore.Api` and one line to its `Program.cs`:
   `builder.AddModule<<M>Module>();` (modules map their endpoints in the order they are added).
5. The migrations live in the host: add `src/Api/NovaCore.Api/<M>/<M>DbContextFactory.cs` (as Catalog's, on
   `DesignTimeDatabase`), the first migration there (root `CLAUDE.md`), and the PostgreSQL test layer's factory and
   migration (`tests/NovaCore.Api.Tests/PostgreSql`).

Use `Catalog` as the reference implementation (full CRUD + contracts); `Payments` shows cross-module use. Together
they show a cross-module write through events both ways (DECISIONS D-034): a payment is saved pending with
`PaymentCreated` (`Payments.Contracts`), Catalog's `ReserveStockForPayment` takes the stock or refuses and answers with
`StockReserved`/`StockReservationFailed` (`Catalog.Contracts`), and Payments' `SettlePayment` completes or fails it.

## Anatomy of a module

- **`Features/`** — one file per feature: `record Request` (validation attributes on its parameters),
  `record Response`, `Map(group)`, and a static `Handle` that takes its dependencies as minimal-API parameters
  (DbContext, contracts, `CancellationToken`). No mediator. `Handle` returns a typed result, e.g.
  `Task<Results<Ok<ProductDto>, ProblemHttpResult>>` from `result.ToHttpResult()`; the union describes the success
  response in OpenAPI (no `.Produces<T>()`), `.ProducesProblem(status)` the failures. `EndpointContractTests` rejects a
  described handler that returns a bare `IResult`.
- **`Domain/`** — entities derive from `AuditableEntity<Guid>` (Guid v7 keys — friendly to SQL Server
  clustering). Keep setters private; mutate through methods.
- **`Infrastructure/`** — `<M>DbContext` (`public const string Schema`, `HasDefaultSchema(Schema)`,
  `ApplyConfigurationsFromAssembly`, `modelBuilder.AddOutbox()`) and an `IEntityTypeConfiguration<>` per entity (each
  ignores `DomainEvents`). Provider-neutral: no provider-specific column types, SQL or functions. The migrations and the
  design-time factory are the host's (they are the chosen provider's).
- **`Contracts/`** — for a module others consume, expose a **separate `*.Contracts` project**
  (interfaces, DTOs and the events it publishes; `NovaCore.Abstractions` only, for `DomainEvent`). Consumers reference the
  Contracts project, **never** the module.
- **`Authorization/`** — `<M>Permissions` constants (`module:resource:action`) + an
  `IPermissionDefinitionProvider`; register with `AddPermissionDefinitions<...>()` in the module.
- **`Mapping/`** — Mapperly static partial mappers; add a `ProjectToDto` IQueryable projection for lists.

## Rules specific to modules

- **DbContext registration:** `services.AddModuleDbContext<<M>DbContext>(<M>DbContext.Schema, (sp, o) =>
  o.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>()));` The host's `AddModuleDatabase` gives the
  provider, connection, retry and migrations assembly; `AddModuleDbContext` keeps the migration history in the schema
  and adds the `OutboxInterceptor` itself; the context maps the table with `modelBuilder.AddOutbox()`.
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
- **A value that concurrent writes change is a concurrency token** (`Product.Stock`: `IsConcurrencyToken()`,
  application-managed so the SQLite tests enforce it too). A write over a value that changed after it was read then
  throws `DbUpdateConcurrencyException` instead of overwriting it. An endpoint answers 409
  (`product.concurrency_conflict`) so the caller reads again; an event handler lets it fail, and the outbox retries the
  delivery on the next pass with a fresh read.
- **Guard every endpoint** with `.RequirePermission(<M>Permissions.X)` (or `.RequireAuthorization()` only
  when no finer permission applies).
- **A tenant's data** (in a host with the multi-tenancy add-on; the repo's modules have none, the template's Todos with
  `--multitenancy` does): the entity implements `ITenantEntity` (`NovaCore.Abstractions`, a public `Guid TenantId` on
  the hierarchy's root, left empty on a new entity), and its endpoints sit under `/api/v1/tenants/{tenant:guid}/...` with
  `.RequireTenant()` (`NovaCore.AspNetCore`) next to the permission. `AddModuleDbContext` filters, fills and guards the
  rows by the scope's tenant; the module neither references the add-on nor writes a `Where(TenantId == ...)`. Start the
  entity's indexes with `TenantId`. Never call `IgnoreQueryFilters()` without names (it fails); a read across tenants
  names the filter (`IgnoreQueryFilters([TenantQueryFilter.Name])`), and work outside a request gives its scope a tenant
  (`TenantContext.Use`). An event handler runs in the tenant that saved the event. README "Çok kiracılılık".
- **Validate write DTOs with DataAnnotations** on the request record (`[Required, MaxLength(200)] string Name`).
  The module's `services.AddValidation()` call makes .NET 10 run them before the handler and answer 400 with a
  validation ProblemDetails. Without that call nothing is validated, and nothing fails either. A rule no built-in
  attribute expresses: a custom `ValidationAttribute` for one value (`CreatePayment`'s `NotEmpty`), `IValidatableObject`
  for several properties. Add the endpoint to `ValidationTests`.
- **Don't reach across modules' data.** Need another module's data? Call its `*.Contracts` interface
  (resolved via DI at runtime) — like `Payments` calling `IProductCatalog`.
- **`WithTags` on the group** requires `using Microsoft.AspNetCore.Http;`.
- After adding/altering an entity, generate a migration for that module's context in the host, and one in the
  PostgreSQL test layer's set (see root `CLAUDE.md`).
