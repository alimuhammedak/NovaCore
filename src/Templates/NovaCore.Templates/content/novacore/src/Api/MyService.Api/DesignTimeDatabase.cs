using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyService.Api;

/// <summary>
/// What <c>dotnet ef</c> builds this host's contexts with, as Program.cs and <c>AddModuleDbContext</c> configure them at
/// run time: the database provider, the migrations in this project, each context's migration history in its own
/// schema. The connection string is only a placeholder: creating a migration or a bundle needs the provider, not a
/// database. Change the provider here and in Program.cs together.
/// </summary>
internal static class DesignTimeDatabase
{
    public static DbContextOptions<TContext> Options<TContext>(string schema)
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
#if (DbSqlServer)
            .UseSqlServer(
                "Server=localhost;Database=MyService;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;",
                sql => sql
                    .MigrationsAssembly(typeof(DesignTimeDatabase).Assembly)
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema))
#else
            .UseNpgsql(
                "Host=localhost;Database=MyService",
                npgsql => npgsql
                    .MigrationsAssembly(typeof(DesignTimeDatabase).Assembly)
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema))
#endif
            .Options;
}
