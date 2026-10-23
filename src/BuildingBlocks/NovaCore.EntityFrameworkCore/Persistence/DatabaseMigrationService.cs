using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NovaCore.EntityFrameworkCore.Persistence;

/// <summary>
/// On startup, applies pending EF Core migrations for every module-owned DbContext that was
/// registered via <see cref="PersistenceExtensions.AddModuleDbContext{TContext}"/>. Opt-in
/// (see <c>Database:ApplyMigrations</c>) so test hosts and DB-less runs are never blocked. The setting is read when the
/// host starts, so configuration added after registration still applies.
/// </summary>
internal sealed partial class DatabaseMigrationService(
    IServiceProvider serviceProvider,
    IEnumerable<ModuleDbContextDescriptor> contexts,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseMigrationService> logger) : IHostedService
{
    /// <summary>Applies pending migrations for each registered module DbContext, one after another.</summary>
    /// <param name="cancellationToken">Cancels the migration.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrations)
            return;

        foreach (var descriptor in contexts)
        {
            var scope = serviceProvider.CreateAsyncScope();
            await using var scopeDisposal = scope.ConfigureAwait(false);
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(descriptor.ContextType);

            LogApplyingMigrations(descriptor.ContextType.Name);
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            LogMigrationsApplied(descriptor.ContextType.Name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Applying migrations for {Context}...")]
    private partial void LogApplyingMigrations(string context);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "{Context} is up to date.")]
    private partial void LogMigrationsApplied(string context);
}
