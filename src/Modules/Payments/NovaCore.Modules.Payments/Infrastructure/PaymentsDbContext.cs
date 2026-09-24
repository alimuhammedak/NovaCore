using Microsoft.EntityFrameworkCore;
using NovaCore.Modules.Payments.Domain;

namespace NovaCore.Modules.Payments.Infrastructure;

/// <summary>The Payments module's DbContext, pinned to the <c>payments</c> schema.</summary>
public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public const string Schema = "payments";

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}
