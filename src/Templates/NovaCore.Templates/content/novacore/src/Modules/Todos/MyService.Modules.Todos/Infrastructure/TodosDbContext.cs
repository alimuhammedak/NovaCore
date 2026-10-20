using Microsoft.EntityFrameworkCore;
using MyService.Modules.Todos.Domain;
using NovaCore.EntityFrameworkCore.Messaging;

namespace MyService.Modules.Todos.Infrastructure;

/// <summary>
/// The Todos module's own DbContext, pinned to the <c>todos</c> schema: each module keeps its own context, schema and
/// migrations, so the module boundary holds in the database too.
/// </summary>
public sealed class TodosDbContext(DbContextOptions<TodosDbContext> options) : DbContext(options)
{
    public const string Schema = "todos";

    public DbSet<Todo> Todos => Set<Todo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TodosDbContext).Assembly);

        // The module's outbox: a domain event an entity raises is saved here in the same transaction as the entity,
        // then delivered to its handlers.
        modelBuilder.AddOutbox();
    }
}
