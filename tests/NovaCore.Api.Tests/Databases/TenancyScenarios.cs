using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NovaCore.EntityFrameworkCore.MultiTenancy;
using Xunit;

namespace NovaCore.Api.Tests.Databases;

/// <summary>
/// Tenant isolation, the same on every provider: a scope reads and writes only its tenant's rows, and none without a
/// tenant (docs/plan/PLAN.md F7b.3, DECISIONS D-076 (5), (6)); a request works in the tenant it names only when the caller
/// is a member of it and it is active (F7b.2, D-076 (3)), and holds there what its roles there grant, per tenant (F7b.4,
/// D-076 (7)); an event's handler works in the tenant that saved the event, in the trace of the request that saved it
/// (F7b.5, D-076 (8)). Every test works with tenants and subjects of its own, so the tests of a class share one database.
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

    [Fact]
    public async Task A_subject_holds_the_permissions_of_its_role_in_each_tenant_and_no_other()
    {
        var alice = Subject();
        var (writes, reads) = (await Fixture.AddTenantAsync(alice), await Fixture.AddTenantAsync());
        await AddMemberAsync(reads, alice, TenancyFixture.Reader);
        using var client = Fixture.ClientFor(alice);

        // Each tenant in turn, then the first again: a tenant's cached permissions never answer for another.
        Assert.Equal([NotesPermissions.Read, NotesPermissions.Write], await PermissionsAsync(client, writes));
        Assert.Equal([NotesPermissions.Read], await PermissionsAsync(client, reads));
        Assert.Equal([NotesPermissions.Read, NotesPermissions.Write], await PermissionsAsync(client, writes));

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(NotesOf(writes), new { text = "a" }, Cancellation)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(NotesOf(reads), new { text = "b" }, Cancellation)).StatusCode);
        Assert.Empty(await TextsAsync(reads));
    }

    [Fact]
    public async Task A_role_change_takes_effect_on_the_next_request()
    {
        var alice = Subject();
        var tenant = await Fixture.AddTenantAsync(alice);
        using var client = Fixture.ClientFor(alice);
        Assert.Equal([NotesPermissions.Read, NotesPermissions.Write], await PermissionsAsync(client, tenant));

        await AddMemberAsync(tenant, alice, TenancyFixture.Reader);

        Assert.Equal([NotesPermissions.Read], await PermissionsAsync(client, tenant));
    }

    [Fact]
    public async Task An_ended_membership_loses_the_tenant_on_the_next_request()
    {
        var alice = Subject();
        var tenant = await Fixture.AddTenantAsync(alice);
        await AddNoteAsync(tenant, "alice's");
        using var client = Fixture.ClientFor(alice);
        Assert.Equal(["alice's"], (await client.GetFromJsonAsync<string[]>(NotesOf(tenant), Cancellation))!);

        await Fixture.InDirectoryAsync(async directory =>
            Assert.True((await directory.RemoveMemberAsync(tenant, alice, Cancellation)).IsSuccess));
        var response = await client.GetAsync(NotesOf(tenant), Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("alice's", await response.Content.ReadAsStringAsync(Cancellation), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_member_without_a_role_works_in_the_tenant_with_no_permission()
    {
        var alice = Subject();
        var tenant = await Fixture.AddTenantAsync();
        await AddMemberAsync(tenant, alice);
        using var client = Fixture.ClientFor(alice);

        Assert.Empty(await PermissionsAsync(client, tenant));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(NotesOf(tenant), Cancellation)).StatusCode);
    }

    [Fact]
    public async Task A_role_outside_the_configuration_is_refused()
    {
        var alice = Subject();
        var tenant = await Fixture.AddTenantAsync();

        var result = await Fixture.InDirectoryAsync(directory =>
            directory.AddMemberAsync(tenant, alice, [TenancyFixture.Reader, "Owner"], Cancellation));

        Assert.Equal("tenant.unknown_role", result.Error.Code);
        Assert.Empty(await Fixture.InDirectoryAsync(directory => directory.TenantsOfAsync(alice, Cancellation)));
    }

    [Fact]
    public async Task The_tenants_of_a_subject_are_its_active_memberships_with_their_roles_by_name()
    {
        var alice = Subject();
        // Opened in another order than their names': the list goes by name.
        var (beta, alpha, suspended) = await Fixture.InDirectoryAsync(async directory => (
            await directory.CreateAsync("Beta", Cancellation),
            await directory.CreateAsync("Alpha", Cancellation),
            await directory.CreateAsync("Suspended", Cancellation)));
        await AddMemberAsync(beta, alice, TenancyFixture.Reader, TenancyFixture.Writer);
        await AddMemberAsync(alpha, alice, TenancyFixture.Reader);
        await AddMemberAsync(alpha, Subject(), TenancyFixture.Writer);
        await AddMemberAsync(suspended, alice, TenancyFixture.Writer);
        await Fixture.InDirectoryAsync(async directory =>
            Assert.True((await directory.SuspendAsync(suspended, Cancellation)).IsSuccess));

        var memberships = await Fixture.InDirectoryAsync(directory => directory.TenantsOfAsync(alice, Cancellation));

        Assert.Equal([alpha, beta], memberships.Select(membership => membership.TenantId));
        Assert.Equal(["Alpha", "Beta"], memberships.Select(membership => membership.Name), StringComparer.Ordinal);
        Assert.Equal([TenancyFixture.Reader], memberships[0].Roles);
        Assert.Equal([TenancyFixture.Reader, TenancyFixture.Writer], memberships[1].Roles);
    }

    [Fact]
    public async Task An_event_handler_works_in_the_tenant_that_saved_the_event_and_continues_the_requests_trace()
    {
        var alice = Subject();
        var (mine, theirs) = (await Fixture.AddTenantAsync(alice), await Fixture.AddTenantAsync());
        await AddNoteAsync(mine, "mine");
        await AddNoteAsync(theirs, "theirs");
        var trace = ActivityTraceId.CreateRandom();
        using var listener = ListenToTrace(trace);
        using var client = Fixture.ClientFor(alice);
        var text = $"added {Guid.NewGuid()}";
        using var request = new HttpRequestMessage(HttpMethod.Post, NotesOf(mine));
        request.Content = JsonContent.Create(new { text });
        request.Headers.Add("traceparent", $"00-{trace}-{ActivitySpanId.CreateRandom()}-01");

        using var created = await client.SendAsync(request, Cancellation);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var handled = await Fixture.HandledAsync(text);

        Assert.Equal(mine, handled.Tenant);
        Assert.Equal([text, "mine"], handled.Texts);
        Assert.Equal(trace, handled.Trace);
    }

    private static string Subject() => Guid.NewGuid().ToString();

    // Records the activities of one trace only, the request's (ASP.NET Core) and the outbox handler's: listeners are
    // process-wide, and the other tests' requests and handlers stay as they were.
    private static ActivityListener ListenToTrace(ActivityTraceId trace)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "Microsoft.AspNetCore" or OutboxDispatcherSource,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Parent.TraceId == trace ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.None
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    // The ActivitySource of the outbox handlers' spans (NovaCore.EntityFrameworkCore, internal).
    private const string OutboxDispatcherSource = "NovaCore.EntityFrameworkCore";

    private static Uri NotesOf(Guid tenant) => new($"/api/v1/tenants/{tenant}/notes", UriKind.Relative);

    private Task AddMemberAsync(Guid tenant, string subject, params string[] roles) =>
        Fixture.InDirectoryAsync(async directory =>
            Assert.True((await directory.AddMemberAsync(tenant, subject, roles, Cancellation)).IsSuccess));

    private static async Task<string[]> PermissionsAsync(HttpClient client, Guid tenant) =>
        (await client.GetFromJsonAsync<string[]>(new Uri($"/api/v1/tenants/{tenant}/permissions", UriKind.Relative), Cancellation))!;

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
