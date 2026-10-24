using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Entities;
using NovaCore.Abstractions.MultiTenancy;
using NovaCore.EntityFrameworkCore.Persistence;
using Xunit;

namespace NovaCore.Api.Tests.Databases;

/// <summary>
/// The core web host (<see cref="CoreWebTestHost"/>) with a module context of tenant entities (<see cref="NotesDbContext"/>),
/// registered the way a module registers its own (<c>AddModuleDbContext</c>), on a database of its own: SQLite in memory,
/// or a database on the SQL Server or PostgreSQL test server. The tables come from <c>EnsureCreated</c>: the model, not a
/// migration, is what these tests are about. Without Docker the host isn't built; the tests skip through
/// <see cref="Database"/>.
/// </summary>
public abstract class TenancyFixture : IAsyncLifetime
{
    private CoreWebTestHost? _host;

    internal TestDatabase Database { get; private set; } = new("a database", null, "not initialized");

    public CoreWebTestHost Host => _host ?? throw new InvalidOperationException("The host isn't started.");

    public async ValueTask InitializeAsync()
    {
        Database = await CreateDatabaseAsync();
        if (!Database.IsAvailable)
            return;

        var connectionString = Database.ConnectionString;
        _host = await CoreWebTestHost.StartAsync(_ => { }, configure: builder =>
        {
            builder.Services.AddModuleDatabase((_, options) => UseDatabase(options, connectionString));
            builder.Services.AddModuleDbContext<NotesDbContext>(NotesDbContext.Schema, (_, _) => { });
        });

        await using var scope = _host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<NotesDbContext>().Database.EnsureCreatedAsync();
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_host is not null)
            await _host.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Runs <paramref name="work"/> on a context of a scope of its own, in <paramref name="tenant"/>, or in none when it is
    /// null; skips the test without Docker.
    /// </summary>
    public async Task<T> InScopeAsync<T>(Guid? tenant, Func<NotesDbContext, Task<T>> work)
    {
        Database.SkipIfUnavailable();
        await using var scope = Host.Services.CreateAsyncScope();
        if (tenant is { } id)
            scope.ServiceProvider.GetRequiredService<TenantContext>().Use(id);

        return await work(scope.ServiceProvider.GetRequiredService<NotesDbContext>());
    }

    /// <summary>A database of its own on the provider's test server, or an in-memory one.</summary>
    private protected abstract Task<TestDatabase> CreateDatabaseAsync();

    /// <summary>Gives a context the provider and <paramref name="connectionString"/>.</summary>
    protected abstract void UseDatabase(DbContextOptionsBuilder options, string connectionString);
}

/// <summary>A module context of tenant entities, for the tenant isolation tests.</summary>
public sealed class NotesDbContext(DbContextOptions<NotesDbContext> options) : DbContext(options)
{
    public const string Schema = "notes";

    /// <summary>The module's own filter, turned off by its name to show that the tenant filter stays on.</summary>
    public const string Unarchived = "Unarchived";

    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<Note>(note =>
        {
            note.ToTable("notes");
            note.Ignore(n => n.DomainEvents);
            note.Property(n => n.Text).HasMaxLength(200);
            note.HasQueryFilter(Unarchived, n => !n.Archived);
        });
    }
}

/// <summary>A note of one tenant.</summary>
public sealed class Note : Entity<Guid>, ITenantEntity
{
    public Note(string text)
        : this(Guid.CreateVersion7(), text)
    {
    }

    /// <summary>A note with a known id, as a stale copy of a saved one.</summary>
    public Note(Guid id, string text)
        : base(id) => Text = text;

    private Note()
    {
    }

    public Guid TenantId { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool Archived { get; set; }
}
