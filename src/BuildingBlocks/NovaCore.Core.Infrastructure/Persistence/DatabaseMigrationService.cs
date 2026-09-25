using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NovaCore.Core.Infrastructure.Persistence;

/// <summary>
/// On startup, applies pending EF Core migrations for every module-owned DbContext that was
/// registered via <see cref="PersistenceExtensions.AddModuleDbContext{TContext}"/>. Opt-in
/// (see <c>Database:ApplyMigrations</c>) so test hosts and DB-less runs are never blocked.
/// </summary>
public sealed partial class DatabaseMigrationService(
    IServiceProvider serviceProvider,
    IEnumerable<ModuleDbContextDescriptor> contexts,
    ILogger<DatabaseMigrationService> logger) : IHostedService
{
    /// <summary>Applies pending migrations for each registered module DbContext, one after another.</summary>
    /// <param name="cancellationToken">Cancels the migration.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var descriptor in contexts)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(descriptor.ContextType);

            LogApplyingMigrations(descriptor.ContextType.Name);
            await context.Database.MigrateAsync(cancellationToken);
            LogMigrationsApplied(descriptor.ContextType.Name);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Applying migrations for {Context}...")]
    private partial void LogApplyingMigrations(string context);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "{Context} is up to date.")]
    private partial void LogMigrationsApplied(string context);
}
