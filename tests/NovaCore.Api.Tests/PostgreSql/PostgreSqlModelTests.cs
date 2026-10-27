using Microsoft.EntityFrameworkCore;
using Xunit;

namespace NovaCore.Api.Tests.PostgreSql;

/// <summary>
/// The PostgreSQL migrations match the model, as CI's <c>has-pending-model-changes</c> checks the host's SQL Server ones:
/// a model change needs a migration in each provider's set (EF Core: "Migrations with Multiple Providers"). No database is
/// needed, so this runs without Docker too.
/// </summary>
public class PostgreSqlModelTests
{
    public static TheoryData<string> Contexts => ["Catalog", "Payments", "Identity", "MultiTenancy"];

    [Theory]
    [MemberData(nameof(Contexts))]
    public void The_PostgreSQL_migrations_match_the_model(string context)
    {
        using DbContext db = context switch
        {
            "Catalog" => new CatalogPostgreSqlFactory().CreateDbContext([]),
            "Payments" => new PaymentsPostgreSqlFactory().CreateDbContext([]),
            "MultiTenancy" => new MultiTenancyPostgreSqlFactory().CreateDbContext([]),
            _ => new IdentityServerPostgreSqlFactory().CreateDbContext([])
        };

        Assert.NotEmpty(db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
