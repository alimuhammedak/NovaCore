using Microsoft.EntityFrameworkCore.Design;
using MyService.Modules.Todos.Infrastructure;

namespace MyService.Api.Todos;

/// <summary>
/// Lets <c>dotnet ef</c> build the Todos module's model without starting the host. The module chooses no database: its
/// migrations live in this project, next to this factory.
/// </summary>
public sealed class TodosDbContextFactory : IDesignTimeDbContextFactory<TodosDbContext>
{
    public TodosDbContext CreateDbContext(string[] args) =>
        new(DesignTimeDatabase.Options<TodosDbContext>(TodosDbContext.Schema));
}
