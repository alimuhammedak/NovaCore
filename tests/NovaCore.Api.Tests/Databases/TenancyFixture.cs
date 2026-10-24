using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
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
/// served under <c>/api/v1/tenants/{tenant}/notes</c> (<c>RequireTenant()</c>), to a member whose role there grants the
/// permission (<see cref="Writer"/>, <see cref="Reader"/>); <c>/api/v1/tenants/{tenant}/permissions</c> shows the
/// caller's. The callers' tokens grant nothing else (<see cref="CoreWebTestHost"/>'s resolver). The tables come from
/// <c>EnsureCreated</c>: the model, not a migration, is what these tests are about. Without Docker the host isn't built;
/// the tests skip through <see cref="Database"/>.
/// </summary>
public abstract class TenancyFixture : IAsyncLifetime
{
    /// <summary>A member's role that reads and writes the tenant's notes (<c>MultiTenancy:Roles</c>).</summary>
    public const string Writer = "Writer";

    /// <summary>A member's role that only reads them.</summary>
    public const string Reader = "Reader";

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
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [$"MultiTenancy:Roles:{Writer}:0"] = NotesPermissions.Read,
                [$"MultiTenancy:Roles:{Writer}:1"] = NotesPermissions.Write,
                [$"MultiTenancy:Roles:{Reader}:0"] = NotesPermissions.Read
            });
            builder.Services.AddPermissionDefinitions<NotesPermissions>();
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

    /// <summary>Opens a tenant with <paramref name="members"/> (subjects) as writers; skips the test without Docker.</summary>
    public async Task<Guid> AddTenantAsync(params string[] members)
    {
        Database.SkipIfUnavailable();
        await using var scope = Host.Services.CreateAsyncScope();
        var directory = scope.ServiceProvider.GetRequiredService<TenantDirectory>();
        var tenant = await directory.CreateAsync("Tenant", TestContext.Current.CancellationToken);
        foreach (var member in members)
            Assert.True((await directory.AddMemberAsync(tenant, member, [Writer], TestContext.Current.CancellationToken)).IsSuccess);

        return tenant;
    }

    /// <summary>Runs <paramref name="change"/> on the tenant directory of a scope of its own; skips the test without Docker.</summary>
    public Task InDirectoryAsync(Func<TenantDirectory, Task> change) =>
        InDirectoryAsync(async directory =>
        {
            await change(directory);
            return true;
        });

    /// <summary>Runs <paramref name="work"/> on the tenant directory of a scope of its own; skips the test without Docker.</summary>
    public async Task<T> InDirectoryAsync<T>(Func<TenantDirectory, Task<T>> work)
    {
        Database.SkipIfUnavailable();
        await using var scope = Host.Services.CreateAsyncScope();
        return await work(scope.ServiceProvider.GetRequiredService<TenantDirectory>());
    }

    /// <summary>A client signed in as <paramref name="subject"/>.</summary>
    public HttpClient ClientFor(string subject) => Host.CreateClientWithClaims(new Claim(NovaClaims.Subject, subject));

    // A module's tenant endpoints: the notes of the request's tenant, and what the caller may do there.
    private static void MapNotes(WebApplication app)
    {
        var tenant = app.MapGroup("/api/v1/tenants/{tenant:guid}").RequireTenant();
        tenant.MapGet("/permissions", (ClaimsPrincipal user) =>
            user.FindAll(NovaClaims.Permission).Select(claim => claim.Value).Order(StringComparer.Ordinal).ToArray());

        var notes = tenant.MapGroup("/notes");
        notes.MapGet("/", async (NotesDbContext context, CancellationToken cancellationToken) =>
                await context.Notes.OrderBy(note => note.Text).Select(note => note.Text).ToListAsync(cancellationToken))
            .RequirePermission(NotesPermissions.Read);
        notes.MapPost("/", async (NoteRequest request, NotesDbContext context, CancellationToken cancellationToken) =>
            {
                context.Notes.Add(new Note(request.Text));
                await context.SaveChangesAsync(cancellationToken);
                return Results.Created();
            })
            .RequirePermission(NotesPermissions.Write);
    }

    /// <summary>A database of its own on the provider's test server, or an in-memory one.</summary>
    private protected abstract Task<TestDatabase> CreateDatabaseAsync();

    /// <summary>Gives a context the provider and <paramref name="connectionString"/>.</summary>
    protected abstract void UseDatabase(DbContextOptionsBuilder options, string connectionString);
}

/// <summary>A note to add over HTTP.</summary>
public sealed record NoteRequest(string Text);

/// <summary>The notes module's permissions, which the tenant's roles grant.</summary>
public sealed class NotesPermissions : IPermissionDefinitionProvider
{
    public const string Read = "notes:notes:read";

    public const string Write = "notes:notes:write";

    public IReadOnlyCollection<PermissionDefinition> GetPermissions() =>
    [
        new(Read, "Read the tenant's notes", "Notes"),
        new(Write, "Write the tenant's notes", "Notes")
    ];
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
