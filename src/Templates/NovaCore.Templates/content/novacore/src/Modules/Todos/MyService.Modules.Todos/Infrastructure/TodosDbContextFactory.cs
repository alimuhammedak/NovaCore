using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyService.Modules.Todos.Infrastructure;

/// <summary>
/// Lets <c>dotnet ef</c> build the model without the host: the module is both the target and the startup project. The
/// connection string is only a placeholder; creating a migration or a bundle needs the provider, not a database.
/// </summary>
public sealed class TodosDbContextFactory : IDesignTimeDbContextFactory<TodosDbContext>
{
    public TodosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TodosDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=MyService;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", TodosDbContext.Schema))
            .Options;

        return new TodosDbContext(options);
    }
}
