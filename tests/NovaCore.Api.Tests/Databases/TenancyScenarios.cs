using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NovaCore.EntityFrameworkCore.MultiTenancy;
using Xunit;

namespace NovaCore.Api.Tests.Databases;

/// <summary>
/// Tenant isolation, the same on every provider: a scope reads and writes only its tenant's rows, and none without a
/// tenant (docs/plan/PLAN.md F7b.3, DECISIONS D-076 (5), (6)); a request works in the tenant it names only when the caller
/// is a member of it and it is active (F7b.2, D-076 (3)). Every test works with tenants and subjects of its own, so the
/// tests of a class share one database.
/// </summary>
public abstract class TenancyScenarios<TFixture>(TFixture fixture) : IClassFixture<TFixture>
    where TFixture : TenancyFixture
{
    protected TFixture Fixture { get; } = fixture;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_scope_reads_only_its_tenants_rows()
    {
        var (mine, theirs) = (Guid.NewGuid(), Guid.NewGuid());
        await AddNoteAsync(mine, "mine");
        await AddNoteAsync(theirs, "theirs");

        Assert.Equal(["mine"], await TextsAsync(mine));
        Assert.Equal(["theirs"], await TextsAsync(theirs));
    }

    [Fact]
    public async Task A_scope_without_a_tenant_reads_no_row()
    {
        await AddNoteAsync(Guid.NewGuid(), "someone's");

        Assert.Empty(await TextsAsync(tenant: null));
    }

    [Fact]
    public async Task A_new_row_gets_the_scopes_tenant()
    {
        var tenant = Guid.NewGuid();

        var id = await AddNoteAsync(tenant, "mine");

        Assert.Equal(tenant, await Fixture.InScopeAsync(tenant, notes => notes.Notes.Where(n => n.Id == id)
            .Select(n => n.TenantId).SingleAsync(Cancellation)));
    }

    [Fact]
    public async Task A_new_row_without_a_tenant_is_refused()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => AddNoteAsync(tenant: null, "nobody's"));

        Assert.Contains("needs a tenant", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_new_row_of_another_tenant_is_refused()
    {
        var (mine, theirs) = (Guid.NewGuid(), Guid.NewGuid());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Fixture.InScopeAsync(mine, notes =>
        {
            notes.Notes.Add(new Note("planted") { TenantId = theirs });
            return notes.SaveChangesAsync(Cancellation);
        }));

        Assert.Contains("another tenant", error.Message, StringComparison.Ordinal);
        Assert.Empty(await TextsAsync(theirs));
    }

    [Fact]
    public async Task Moving_a_row_to_another_tenant_is_refused()
    {
        var (mine, theirs) = (Guid.NewGuid(), Guid.NewGuid());
        var id = await AddNoteAsync(mine, "mine");

        await Assert.ThrowsAsync<InvalidOperationException>(() => Fixture.InScopeAsync(mine, async notes =>
        {
            var note = await notes.Notes.SingleAsync(n => n.Id == id, Cancellation);
            note.TenantId = theirs;
            return await notes.SaveChangesAsync(Cancellation);
        }));

        Assert.Equal(["mine"], await TextsAsync(mine));
        Assert.Empty(await TextsAsync(theirs));
    }

    [Fact]
    public async Task Changing_or_deleting_another_tenants_row_is_refused()
    {
        var (mine, theirs) = (Guid.NewGuid(), Guid.NewGuid());
        var id = await AddNoteAsync(theirs, "theirs");

        // The filter keeps the row out of a query, so the change comes from an entity attached by hand.
        await Assert.ThrowsAsync<InvalidOperationException>(() => Fixture.InScopeAsync(mine, notes =>
        {
            notes.Notes.Attach(new Note(id, "overwritten") { TenantId = theirs }).Property(n => n.Text).IsModified = true;
            return notes.SaveChangesAsync(Cancellation);
        }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Fixture.InScopeAsync(mine, notes =>
        {
            notes.Notes.Remove(new Note(id, "theirs") { TenantId = theirs });
            return notes.SaveChangesAsync(Cancellation);
        }));

        Assert.Equal(["theirs"], await TextsAsync(theirs));
    }

    [Fact]
    public async Task Turning_another_filter_off_by_its_name_keeps_the_tenant_filter()
    {
        var (mine, theirs) = (Guid.NewGuid(), Guid.NewGuid());
        await AddNoteAsync(mine, "mine, archived", archived: true);
        await AddNoteAsync(theirs, "theirs, archived", archived: true);

        var texts = await Fixture.InScopeAsync(mine, notes => notes.Notes
            .IgnoreQueryFilters([NotesDbContext.Unarchived])
            .Select(n => n.Text)
            .ToListAsync(Cancellation));

        Assert.Equal(["mine, archived"], texts);
    }

    [Fact]
    public async Task Turning_every_filter_off_at_once_is_refused()
    {
        var tenant = Guid.NewGuid();
        await AddNoteAsync(tenant, "mine");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Fixture.InScopeAsync(tenant,
            notes => notes.Notes.IgnoreQueryFilters().ToListAsync(Cancellation)));

        Assert.Contains(nameof(TenantQueryFilter.Name), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_read_across_tenants_turns_the_tenant_filter_off_by_its_name()
    {
        var (first, second) = (Guid.NewGuid(), Guid.NewGuid());
        await AddNoteAsync(first, "first");
        await AddNoteAsync(second, "second");

        var tenants = await Fixture.InScopeAsync(tenant: null, notes => notes.Notes
            .IgnoreQueryFilters([TenantQueryFilter.Name])
            .Where(n => n.TenantId == first || n.TenantId == second)
            .Select(n => n.TenantId)
            .ToListAsync(Cancellation));

        Assert.Equal(new[] { first, second }.Order(), tenants.Order());
    }

    [Fact]
    public async Task Bulk_updates_and_deletes_reach_only_the_scopes_tenant()
    {
        var (mine, theirs) = (Guid.NewGuid(), Guid.NewGuid());
        await AddNoteAsync(mine, "mine");
        await AddNoteAsync(theirs, "theirs");

        await Fixture.InScopeAsync(mine, notes => notes.Notes.ExecuteUpdateAsync(
            set => set.SetProperty(n => n.Text, "updated"), Cancellation));
        Assert.Equal(["updated"], await TextsAsync(mine));
        Assert.Equal(["theirs"], await TextsAsync(theirs));

        await Fixture.InScopeAsync(mine, notes => notes.Notes.ExecuteDeleteAsync(Cancellation));
        Assert.Empty(await TextsAsync(mine));
        Assert.Equal(["theirs"], await TextsAsync(theirs));
    }

    [Fact]
    public async Task A_member_works_in_their_tenant()
    {
        var alice = Subject();
        var tenant = await Fixture.AddTenantAsync(alice);
        using var client = Fixture.ClientFor(alice);

        var created = await client.PostAsJsonAsync(NotesOf(tenant), new { text = "alice's" }, Cancellation);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(["alice's"], (await client.GetFromJsonAsync<string[]>(NotesOf(tenant), Cancellation))!);
    }

    [Fact]
    public async Task A_caller_who_is_not_a_member_gets_403_and_none_of_the_tenants_data()
    {
        var (alice, bob) = (Subject(), Subject());
        var bobs = await Fixture.AddTenantAsync(bob);
        await AddNoteAsync(bobs, "bob's");
        using var client = Fixture.ClientFor(alice);

        var response = await client.GetAsync(NotesOf(bobs), Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("bob's", await response.Content.ReadAsStringAsync(Cancellation), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Another_tenant_in_the_path_with_a_valid_token_gives_no_access()
    {
        var (alice, bob) = (Subject(), Subject());
        await Fixture.AddTenantAsync(alice);
        var bobs = await Fixture.AddTenantAsync(bob);
        using var client = Fixture.ClientFor(alice);

        var write = await client.PostAsJsonAsync(NotesOf(bobs), new { text = "planted" }, Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        Assert.Empty(await TextsAsync(bobs));
    }

    [Fact]
    public async Task A_tenant_that_doesnt_exist_gets_the_same_403()
    {
        using var client = Fixture.ClientFor(Subject());

        var response = await client.GetAsync(NotesOf(Guid.NewGuid()), Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_suspended_tenant_shows_no_data_even_after_a_cached_access()
    {
        var alice = Subject();
        var tenant = await Fixture.AddTenantAsync(alice);
        await AddNoteAsync(tenant, "alice's");
        using var client = Fixture.ClientFor(alice);
        Assert.Equal(["alice's"], (await client.GetFromJsonAsync<string[]>(NotesOf(tenant), Cancellation))!);

        await Fixture.InDirectoryAsync(async directory =>
            Assert.True((await directory.SuspendAsync(tenant, Cancellation)).IsSuccess));
        var response = await client.GetAsync(NotesOf(tenant), Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("alice's", await response.Content.ReadAsStringAsync(Cancellation), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_anonymous_request_gets_no_tenant()
    {
        var tenant = await Fixture.AddTenantAsync(Subject());
        using var client = Fixture.Host.CreateClient();

        var response = await client.GetAsync(NotesOf(tenant), Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string Subject() => Guid.NewGuid().ToString();

    private static Uri NotesOf(Guid tenant) => new($"/api/v1/tenants/{tenant}/notes", UriKind.Relative);

    private Task<Guid> AddNoteAsync(Guid? tenant, string text, bool archived = false) =>
        Fixture.InScopeAsync(tenant, async notes =>
        {
            var note = new Note(text) { Archived = archived };
            notes.Notes.Add(note);
            await notes.SaveChangesAsync(Cancellation);
            return note.Id;
        });

    private Task<List<string>> TextsAsync(Guid? tenant) =>
        Fixture.InScopeAsync(tenant, notes => notes.Notes.Select(n => n.Text).ToListAsync(Cancellation));
}
