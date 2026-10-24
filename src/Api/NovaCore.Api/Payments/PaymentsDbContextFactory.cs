using Microsoft.EntityFrameworkCore.Design;
using NovaCore.Modules.Payments.Infrastructure;

namespace NovaCore.Api.Payments;

/// <summary>
/// Lets <c>dotnet ef</c> build the Payments module's model offline. The module chooses no database: its SQL Server
/// migrations live in this host, next to this factory.
/// </summary>
public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args) =>
        new(DesignTimeDatabase.Options<PaymentsDbContext>(PaymentsDbContext.Schema));
}
