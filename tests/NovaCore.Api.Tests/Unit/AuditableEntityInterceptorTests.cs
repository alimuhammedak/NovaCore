using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NovaCore.Abstractions.Entities;
using NovaCore.Abstractions.Identity;
using NovaCore.EntityFrameworkCore.Persistence;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

public class AuditableEntityInterceptorTests
{
    [Fact]
    public async Task Audit_stamps_come_from_the_injected_clock()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new AuditableEntityInterceptor(new SignedInUser("alice-subject", "alice@example.com"), clock))
            .Options;
        await using var context = new AuditDbContext(options);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var item = new AuditedItem(Guid.NewGuid()) { Name = "first" };
        context.Items.Add(item);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var created = clock.GetUtcNow();

        clock.Advance(TimeSpan.FromHours(1));
        item.Name = "second";
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(created, item.CreatedAtUtc);
        Assert.Equal(clock.GetUtcNow(), item.ModifiedAtUtc);
    }

    [Fact]
    public async Task Audit_stamps_name_the_caller_by_their_subject_not_their_user_name()
    {
        // The subject never changes and isn't personal data; the identity provider's user name is the email address
        // (DECISIONS D-054 (11), PLAN F7b.7).
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new AuditableEntityInterceptor(new SignedInUser("alice-subject", "alice@example.com"), TimeProvider.System))
            .Options;
        await using var context = new AuditDbContext(options);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var item = new AuditedItem(Guid.NewGuid()) { Name = "first" };
        context.Items.Add(item);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        item.Name = "second";
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal("alice-subject", item.CreatedBy);
        Assert.Equal("alice-subject", item.ModifiedBy);
    }

    private sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
    {
        public DbSet<AuditedItem> Items => Set<AuditedItem>();
    }

    private sealed class AuditedItem(Guid id) : AuditableEntity<Guid>(id)
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class SignedInUser(string subject, string userName) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public string? UserId => subject;
        public string? UserName => userName;
        public string? Email => null;
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => false;
    }
}
