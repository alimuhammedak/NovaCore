using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NovaCore.Modules.Payments.Infrastructure;

/// <summary>Design-time factory so <c>dotnet ef migrations</c> can build the model without a host or DB.</summary>
public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=NovaCore;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", PaymentsDbContext.Schema))
            .Options;

        return new PaymentsDbContext(options);
    }
}
