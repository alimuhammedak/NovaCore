using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Abstractions.Entities;
using NovaCore.Abstractions.MultiTenancy;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.MultiTenancy;
using Xunit;

namespace NovaCore.Api.Tests.Databases;

/// <summary>
/// The core web host (<see cref="CoreWebTestHost"/>) with the multi-tenancy add-on and a module context of tenant entities
/// (<see cref="NotesDbContext"/>), registered the way a module registers its own (<c>AddModuleDbContext</c>), on a
/// database of its own: SQLite in memory, or a database on the SQL Server or PostgreSQL test server. A tenant's notes are
/// served under <c>/api/v1/tenants/{tenant}/notes</c> (<c>RequireTenant()</c>). The tables come from
/// <c>EnsureCreated</c>: the model, not a migration, is what these tests are about. Without Docker the host isn't built;
/// the tests skip through <see cref="Database"/>.
/// </summary>
public abstract class TenancyFixture : IAsyncLifetime
{
    private CoreWebTestHost? _host;

    internal TestDatabase Database { get; private set; } = new("a database", null, "not initialized");

    /// <summary>The host; skips the calling test without Docker, as every use of the fixture does.</summary>
    public CoreWebTestHost Host
    {
        get
        {
            Database.SkipIfUnavailable();
            return _host ?? throw new InvalidOperationException("The host isn't started.");
        }
    }

    public async ValueTask InitializeAsync()
    {
        Database = await CreateDatabaseAsync();
        if (!Database.IsAvailable)
            return;

        var connectionString = Database.ConnectionString;
        _host = await CoreWebTestHost.StartAsync(MapNotes, configure: builder =>
        {
            builder.AddCoreMultiTenancy();
            builder.Services.AddModuleDatabase((_, options) => UseDatabase(options, connectionString));
            builder.Services.AddModuleDbContext<NotesDbContext>(NotesDbContext.Schema, (_, _) => { });
            builder.Services.AddModuleDbContext<MultiTenancyDbContext>(MultiTenancyDbContext.Schema, (_, _) => { });
        });

        // Both contexts in one database: the first creates it, the second adds its tables.
        await using var scope = _host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<NotesDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<MultiTenancyDbContext>().GetService<IRelationalDatabaseCreator>()
            .CreateTablesAsync();
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

    /// <summary>Opens a tenant with <paramref name="members"/> (subjects), without roles; skips the test without Docker.</summary>
    public async Task<Guid> AddTenantAsync(params string[] members)
    {
        Database.SkipIfUnavailable();
        await using var scope = Host.Services.CreateAsyncScope();
        var directory = scope.ServiceProvider.GetRequiredService<TenantDirectory>();
        var tenant = await directory.CreateAsync("Tenant", TestContext.Current.CancellationToken);
        foreach (var member in members)
            Assert.True((await directory.AddMemberAsync(tenant, member, [], TestContext.Current.CancellationToken)).IsSuccess);

        return tenant;
    }

    /// <summary>Runs <paramref name="change"/> on the tenant directory of a scope of its own.</summary>
    public async Task InDirectoryAsync(Func<TenantDirectory, Task> change)
    {
        await using var scope = Host.Services.CreateAsyncScope();
        await change(scope.ServiceProvider.GetRequiredService<TenantDirectory>());
    }

    /// <summary>A client signed in as <paramref name="subject"/>.</summary>
    public HttpClient ClientFor(string subject) => Host.CreateClientWithClaims(new Claim(NovaClaims.Subject, subject));

    // A module's tenant endpoints: the notes of the request's tenant.
    private static void MapNotes(WebApplication app)
    {
        var notes = app.MapGroup("/api/v1/tenants/{tenant:guid}/notes").RequireTenant();
        notes.MapGet("/", async (NotesDbContext context, CancellationToken cancellationToken) =>
            await context.Notes.OrderBy(note => note.Text).Select(note => note.Text).ToListAsync(cancellationToken));
        notes.MapPost("/", async (NoteRequest request, NotesDbContext context, CancellationToken cancellationToken) =>
        {
            context.Notes.Add(new Note(request.Text));
            await context.SaveChangesAsync(cancellationToken);
            return Results.Created();
        });
    }

    /// <summary>A database of its own on the provider's test server, or an in-memory one.</summary>
    private protected abstract Task<TestDatabase> CreateDatabaseAsync();

    /// <summary>Gives a context the provider and <paramref name="connectionString"/>.</summary>
    protected abstract void UseDatabase(DbContextOptionsBuilder options, string connectionString);
}

/// <summary>A note to add over HTTP.</summary>
public sealed record NoteRequest(string Text);

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
