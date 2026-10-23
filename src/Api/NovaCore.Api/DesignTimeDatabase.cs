using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NovaCore.Api;

/// <summary>
/// What <c>dotnet ef</c> builds this host's contexts with, as Program and <c>AddModuleDbContext</c> configure them at run
/// time: SQL Server, the migrations in this assembly, each context's migration history in its own schema. Offline: the
/// connection string is a placeholder, migrations only need the provider to shape their SQL.
/// </summary>
internal static class DesignTimeDatabase
{
    public static DbContextOptions<TContext> Options<TContext>(string schema)
        where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(
                "Server=localhost;Database=NovaCore;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;",
                sql => sql
                    .MigrationsAssembly(typeof(DesignTimeDatabase).Assembly)
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema))
            .Options;
}
