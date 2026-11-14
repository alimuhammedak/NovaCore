using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Account invitations (PLAN F8.6, D-091 (5), D-095): an invitation is accepted once and sets the password; a used and an
/// expired one are refused with codes of their own; only a caller with <c>identity:invitations:create</c> invites; an
/// invitation never changes the password of an existing account.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed partial class InvitationTests(InvitationTests.Fixture fixture) : IClassFixture<InvitationTests.Fixture>
{
    private const string Password = "a long passphrase for tests";
    private const string NewPassword = "an invited passphrase";
    private const string Status = "/api/v1/auth/invitations/status";
    private const string Accept = "/api/v1/auth/invitations/accept";
    private const string Create = "/api/v1/admin/invitations";

    private readonly IdentityServerTestHost _host = fixture.Host;

    public sealed class Fixture : IdentityServerFixture
    {
        public CapturingEmailSender Emails { get; } = new();

        public AccountCreationRace Creations { get; } = new();

        public ConcurrentUserWrite Writes { get; } = new();

        public InvitationAcceptRace Accepts { get; } = new();

        public LogCapture Log { get; } = new();

        protected override void Configure(WebApplicationBuilder builder)
        {
            builder.Logging.AddProvider(Log);
            builder.Services.AddSingleton<IEmailSender>(Emails);
            builder.Services.ConfigureDbContext<IdentityServerDbContext>(options => options.AddInterceptors(Creations, Writes, Accepts));
        }
    }

    [Fact]
    public async Task An_invitation_is_accepted_once_and_sets_the_password()
    {
        using var client = await InviterAsync("inviter-once@novacore.test");

        var created = await client.PostAsJsonAsync(new Uri(Create, UriKind.Relative), new { email = "invited-once@novacore.test" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var token = await TokenSentToAsync(fixture.Emails, "invited-once@novacore.test");

        var status = await PostAsync(client, Status, new { token });
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        using (var body = JsonDocument.Parse(await status.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)))
            Assert.Equal("invited-once@novacore.test", body.RootElement.GetProperty("email").GetString());

        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, Accept, new { token, password = NewPassword })).StatusCode);
        using (var browser = new OidcFlow(_host))
            Assert.False(string.IsNullOrEmpty((await browser.SignInAsync("invited-once@novacore.test", NewPassword)).AccessToken));

        await AssertProblemAsync(await PostAsync(client, Accept, new { token, password = "yet another passphrase" }), HttpStatusCode.Conflict, "invitation.used");
        await AssertProblemAsync(await PostAsync(client, Status, new { token }), HttpStatusCode.Conflict, "invitation.used");
    }

    // The audit trail names who invited, as it does for a role change (S-5): by the account's id, never its address (Y-35).
    [Fact]
    public async Task The_audit_log_names_the_inviter()
    {
        var inviter = await _host.CreateUserAsync("inviter-audit@novacore.test", Password, "identity:invitations:create");
        using var browser = new OidcFlow(_host);
        using var client = _host.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", (await browser.SignInAsync("inviter-audit@novacore.test", Password)).AccessToken);

        var created = await PostAsync(client, Create, new { email = "invited-audit@novacore.test" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        using var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var invited = body.RootElement.GetProperty("userId").GetGuid();

        var entry = Assert.Single(fixture.Log.Messages, message => message.StartsWith($"SECURITY invitation_created User={invited}", StringComparison.Ordinal));
        Assert.Equal($"SECURITY invitation_created User={invited} By={inviter.Id}", entry);
    }

    [Fact]
    public async Task An_unknown_token_is_not_found()
    {
        using var client = _host.CreateClient();

        await AssertProblemAsync(await PostAsync(client, Status, new { token = "not-an-invitation" }), HttpStatusCode.NotFound, "invitation.not_found");
        await AssertProblemAsync(await PostAsync(client, Accept, new { token = "not-an-invitation", password = NewPassword }), HttpStatusCode.NotFound, "invitation.not_found");
    }

    [Fact]
    public async Task A_caller_without_the_permission_cannot_invite()
    {
        await _host.CreateUserAsync("not-an-inviter@novacore.test", Password);
        using var browser = new OidcFlow(_host);
        var tokens = await browser.SignInAsync("not-an-inviter@novacore.test", Password);
        using var client = _host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var response = await client.PostAsJsonAsync(new Uri(Create, UriKind.Relative), new { email = "uninvited@novacore.test" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var anonymous = _host.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync(new Uri(Create, UriKind.Relative), new { email = "uninvited@novacore.test" }, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task An_invitation_cannot_take_over_an_existing_account()
    {
        var existing = await _host.CreateUserAsync("existing-invited@novacore.test", Password);
        using var client = await InviterAsync("inviter-existing@novacore.test");

        var created = await client.PostAsJsonAsync(new Uri(Create, UriKind.Relative), new { email = existing.Email }, TestContext.Current.CancellationToken);
        using (var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)))
            Assert.Equal(existing.Id, body.RootElement.GetProperty("userId").GetGuid());
        var token = await TokenSentToAsync(fixture.Emails, existing.Email!);

        await AssertProblemAsync(await PostAsync(client, Accept, new { token, password = NewPassword }), HttpStatusCode.Conflict, "invitation.account_exists");

        using var browser = new OidcFlow(_host);
        Assert.False(string.IsNullOrEmpty((await browser.SignInAsync(existing.Email!, Password)).AccessToken));
    }

    // [EmailAddress] lets '#' through; Identity's AllowedUserNameCharacters don't (the address is the user name).
    [Fact]
    public async Task An_address_the_account_rules_refuse_is_a_bad_request_not_a_server_error()
    {
        using var client = await InviterAsync("inviter-invalid@novacore.test");

        var created = await client.PostAsJsonAsync(new Uri(Create, UriKind.Relative), new { email = "a#b@novacore.test" }, TestContext.Current.CancellationToken);

        await AssertProblemAsync(created, HttpStatusCode.BadRequest, "invitation.invalid_email");
    }

    // Another account holds the address as its user name: the address can't become an account of its own.
    [Fact]
    public async Task An_address_another_account_holds_as_its_user_name_is_a_conflict()
    {
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.CreateAsync(new ApplicationUser
            {
                UserName = "held-name@novacore.test",
                Email = "held-elsewhere@novacore.test",
                CreatedAtUtc = TimeProvider.System.GetUtcNow()
            })).Succeeded);
        }

        using var client = await InviterAsync("inviter-held@novacore.test");

        var created = await client.PostAsJsonAsync(new Uri(Create, UriKind.Relative), new { email = "held-name@novacore.test" }, TestContext.Current.CancellationToken);

        await AssertProblemAsync(created, HttpStatusCode.Conflict, "invitation.address_taken");
    }

    // Both requests pass Identity's user validation before either inserts (AccountCreationRace): the one that loses the
    // insert invites the account the other created, as it would have a moment later.
    [Fact]
    public async Task Concurrent_invitations_to_one_new_address_invite_one_account()
    {
        using var client = await InviterAsync("inviter-race@novacore.test");

        fixture.Creations.Arm(2);
        var answers = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            client.PostAsJsonAsync(new Uri(Create, UriKind.Relative), new { email = "invited-race@novacore.test" }, TestContext.Current.CancellationToken)));

        var ids = new List<Guid>();
        foreach (var answer in answers)
        {
            Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
            using var body = JsonDocument.Parse(await answer.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            ids.Add(body.RootElement.GetProperty("userId").GetGuid());
        }

        Assert.Single(ids.Distinct());
        await fixture.Emails.WaitForAsync(message => string.Equals(message.To, "invited-race@novacore.test", StringComparison.Ordinal), 2);
    }

    // The invitation is marked used before the password is set; a password save lost to a concurrent write of the
    // account (ConcurrentUserWrite) must not leave the link used up with no password set (V8-6).
    [Fact]
    public async Task A_password_step_lost_to_a_concurrent_write_leaves_the_invitation_unused()
    {
        await using (var scope = _host.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<AccountInvitations>().InviteAsync("invited-lost@novacore.test", TestContext.Current.CancellationToken);
        var token = await TokenSentToAsync(fixture.Emails, "invited-lost@novacore.test");
        using var client = _host.CreateClient();

        fixture.Writes.Arm();
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(client, Accept, new { token, password = NewPassword })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, Status, new { token })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, Accept, new { token, password = NewPassword })).StatusCode);
        using var browser = new OidcFlow(_host);
        Assert.False(string.IsNullOrEmpty((await browser.SignInAsync("invited-lost@novacore.test", NewPassword)).AccessToken));
    }

    // The invited account opens unconfirmed and without a password (D-091 (5)): its status is pending, and even a
    // password it got some other way doesn't sign it in before the invitation confirms the address.
    [Fact]
    public async Task An_invited_account_cannot_sign_in_before_accepting()
    {
        const string email = "not-yet@novacore.test";
        await using (var scope = _host.Services.CreateAsyncScope())
            Assert.True((await scope.ServiceProvider.GetRequiredService<AccountInvitations>().InviteAsync(email, TestContext.Current.CancellationToken)).IsSuccess);
        var token = await TokenSentToAsync(fixture.Emails, email);
        using var client = _host.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, Status, new { token })).StatusCode);

        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var invited = (await users.FindByEmailAsync(email))!;
            Assert.False(invited.EmailConfirmed);
            Assert.False(await users.HasPasswordAsync(invited));
            Assert.True((await users.AddPasswordAsync(invited, NewPassword)).Succeeded);
        }

        using var browser = new OidcFlow(_host);
        var (_, afterPassword) = await browser.BeginSignInAsync(email, NewPassword);
        Assert.NotEqual(HttpStatusCode.Redirect, afterPassword.StatusCode);
        Assert.Contains("Confirm your email address before signing in.", await afterPassword.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    // Both requests find the invitation pending before either opens its transaction (InvitationAcceptRace): the
    // conditional update lets one through, the other is told the invitation is used (D-095 (4)). Without its
    // AcceptedAtUtc condition the other one marks it too and fails later, on the account's concurrency stamp.
    [Fact]
    public async Task Concurrent_accepts_of_one_invitation_accept_it_once()
    {
        await using (var scope = _host.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<AccountInvitations>().InviteAsync("invited-twice@novacore.test", TestContext.Current.CancellationToken);
        var token = await TokenSentToAsync(fixture.Emails, "invited-twice@novacore.test");
        using var client = _host.CreateClient();

        fixture.Accepts.Arm(2);
        var answers = await Task.WhenAll(
            PostAsync(client, Accept, new { token, password = NewPassword }),
            PostAsync(client, Accept, new { token, password = "the other passphrase here" }));

        var accepted = Assert.Single(answers, answer => answer.StatusCode == HttpStatusCode.OK);
        await AssertProblemAsync(Assert.Single(answers, answer => answer != accepted), HttpStatusCode.Conflict, "invitation.used");
    }

    // D-091 (5): an unconfirmed account that exists (registered, never confirmed) keeps its id; accepting sets the new
    // password and confirms the address: holding the link is holding the mailbox.
    [Fact]
    public async Task An_unconfirmed_account_with_a_password_gets_the_new_password_and_is_confirmed()
    {
        const string email = "unconfirmed-invited@novacore.test";
        Guid id;
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var registered = new ApplicationUser { UserName = email, Email = email, CreatedAtUtc = TimeProvider.System.GetUtcNow() };
            Assert.True((await users.CreateAsync(registered, Password)).Succeeded);
            id = registered.Id;
        }

        using var client = await InviterAsync("inviter-unconfirmed@novacore.test");
        var created = await client.PostAsJsonAsync(new Uri(Create, UriKind.Relative), new { email }, TestContext.Current.CancellationToken);
        using (var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)))
            Assert.Equal(id, body.RootElement.GetProperty("userId").GetGuid());
        var token = await TokenSentToAsync(fixture.Emails, email);

        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, Status, new { token })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, Accept, new { token, password = NewPassword })).StatusCode);

        using (var browser = new OidcFlow(_host))
            Assert.False(string.IsNullOrEmpty((await browser.SignInAsync(email, NewPassword)).AccessToken));
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var accepted = (await users.FindByEmailAsync(email))!;
            Assert.True(accepted.EmailConfirmed);
            Assert.False(await users.CheckPasswordAsync(accepted, Password));
        }
    }

    // RFC 9700 §2.6, D-095 (6): the invitation endpoints answer no other origin, while the same host's token endpoint,
    // which keeps CORS for single-page apps, answers it: the configured origin works, the endpoints opt out.
    [Fact]
    public async Task The_invitation_endpoints_answer_no_cross_origin_request_where_the_token_endpoint_does()
    {
        const string origin = "https://app.novacore.test";
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { ["Cors:AllowedOrigins:0"] = origin }));
        using var client = host.CreateClient();

        Assert.True(await AllowsOriginAsync(client, HttpMethod.Options, "/connect/token", origin));
        Assert.True(await AllowsOriginAsync(client, HttpMethod.Post, "/connect/token", origin));
        foreach (var path in new[] { Status, Accept })
        {
            Assert.False(await AllowsOriginAsync(client, HttpMethod.Options, path, origin));
            Assert.False(await AllowsOriginAsync(client, HttpMethod.Post, path, origin));
        }
    }

    // D-095 (2): the link leads to the configured screen of this host; a path that could leave it stops the host.
    [Fact]
    public async Task The_link_leads_to_the_configured_invitation_path()
    {
        var emails = new CapturingEmailSender();
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
        {
            builder.Services.AddSingleton<IEmailSender>(emails);
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { ["Identity:SignIn:InvitationPath"] = "/join" });
        });
        await using (var scope = host.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<AccountInvitations>().InviteAsync("joining@novacore.test", TestContext.Current.CancellationToken);

        var sent = await emails.WaitForAsync(message => string.Equals(message.To, "joining@novacore.test", StringComparison.Ordinal));
        var link = new Uri(WebUtility.HtmlDecode(Link().Match(sent[0].HtmlBody).Groups["link"].Value));
        Assert.Equal(new Uri(IdentityServerTestHost.Authority).Authority, link.Authority);
        Assert.Equal("/join", link.AbsolutePath);
    }

    [Theory]
    [InlineData("https://evil.novacore.test/join")]
    [InlineData("//evil.novacore.test/join")]
    [InlineData("/\\evil.novacore.test/join")]
    [InlineData("join")]
    [InlineData("~/join")]
    [InlineData("/join?next=1")]
    [InlineData("/join#top")]
    public async Task An_invitation_path_that_could_leave_this_host_stops_the_host(string invitationPath)
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => IdentityServerTestHost.StartAsync(configure: builder =>
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { ["Identity:SignIn:InvitationPath"] = invitationPath })));

        Assert.Contains("Identity:SignIn:InvitationPath", exception.ToString(), StringComparison.Ordinal);
    }

    // Its own host: the clock moves, and this test signs nobody in (tests/CLAUDE.md).
    [Fact]
    public async Task An_expired_invitation_is_refused_with_its_own_code()
    {
        var clock = new FakeTimeProvider(TimeProvider.System.GetUtcNow());
        var emails = new CapturingEmailSender();
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
        {
            builder.Services.AddSingleton<TimeProvider>(clock);
            builder.Services.AddSingleton<IEmailSender>(emails);
        });
        await using (var scope = host.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<AccountInvitations>().InviteAsync("expired-invite@novacore.test", TestContext.Current.CancellationToken);
        var token = await TokenSentToAsync(emails, "expired-invite@novacore.test");
        using var client = host.CreateClient();

        clock.Advance(AccountInvitations.Lifetime + TimeSpan.FromSeconds(1));

        await AssertProblemAsync(await PostAsync(client, Status, new { token }), HttpStatusCode.Gone, "invitation.expired");
        await AssertProblemAsync(await PostAsync(client, Accept, new { token, password = NewPassword }), HttpStatusCode.Gone, "invitation.expired");
    }

    // S-2: an invited account has no password until the invitation sets one. Anyone may ask for the confirmation again;
    // whatever reaches the mailbox must not confirm the account without a password, or the invitation becomes useless.
    [Fact]
    public async Task Resending_the_confirmation_leaves_the_invitation_usable()
    {
        using var client = await InviterAsync("inviter-resend@novacore.test");
        await client.PostAsJsonAsync(new Uri(Create, UriKind.Relative), new { email = "invited-resend@novacore.test" }, TestContext.Current.CancellationToken);
        var token = await TokenSentToAsync(fixture.Emails, "invited-resend@novacore.test");

        using var anonymous = _host.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(anonymous, "/api/v1/auth/resend-confirmation-email", new { email = "invited-resend@novacore.test" })).StatusCode);

        // The emails go out in the order queued: once a later one has gone, the resent one would have too.
        await client.PostAsJsonAsync(new Uri(Create, UriKind.Relative), new { email = "after-invited-resend@novacore.test" }, TestContext.Current.CancellationToken);
        await fixture.Emails.WaitForAsync(message => string.Equals(message.To, "after-invited-resend@novacore.test", StringComparison.Ordinal));
        Assert.Single(fixture.Emails.Sent, message => string.Equals(message.To, "invited-resend@novacore.test", StringComparison.Ordinal));

        Assert.Equal(HttpStatusCode.OK, (await PostAsync(anonymous, Status, new { token })).StatusCode);
    }

    [Fact]
    public async Task A_confirmation_link_does_not_confirm_an_account_without_a_password()
    {
        using var client = await InviterAsync("inviter-confirm@novacore.test");
        await client.PostAsJsonAsync(new Uri(Create, UriKind.Relative), new { email = "invited-confirm@novacore.test" }, TestContext.Current.CancellationToken);
        var token = await TokenSentToAsync(fixture.Emails, "invited-confirm@novacore.test");

        string link;
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync("invited-confirm@novacore.test");
            Assert.NotNull(user);
            var confirmation = Base64UrlTextEncoder.Encode(System.Text.Encoding.UTF8.GetBytes(await users.GenerateEmailConfirmationTokenAsync(user)));
            link = $"/api/v1/auth/confirm-email?userId={user.Id}&token={confirmation}";
        }

        using var anonymous = _host.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync(new Uri(link, UriKind.Relative), TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(anonymous, Status, new { token })).StatusCode);
    }

    private async Task<HttpClient> InviterAsync(string email)
    {
        await _host.CreateUserAsync(email, Password, "identity:invitations:create");
        using var browser = new OidcFlow(_host);
        var tokens = await browser.SignInAsync(email, Password);
        var client = _host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    private static async Task<string> TokenSentToAsync(CapturingEmailSender emails, string email)
    {
        var sent = await emails.WaitForAsync(message => string.Equals(message.To, email, StringComparison.Ordinal));
        var link = new Uri(WebUtility.HtmlDecode(Link().Match(sent[0].HtmlBody).Groups["link"].Value));
        Assert.Equal("/accept-invitation", link.AbsolutePath);
        return QueryHelpers.ParseQuery(link.Query)["token"].ToString();
    }

    // A preflight, or the request a script on that origin sends.
    private static async Task<bool> AllowsOriginAsync(HttpClient client, HttpMethod method, string path, string origin)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Origin", origin);
        if (method == HttpMethod.Options)
            request.Headers.Add("Access-Control-Request-Method", "POST");
        else
            request.Content = path.StartsWith("/connect", StringComparison.Ordinal)
                ? new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal) { ["grant_type"] = "authorization_code" })
                : JsonContent.Create(new { token = "not-an-invitation", password = NewPassword });

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        return response.Headers.Contains("Access-Control-Allow-Origin");
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, object body) =>
        await client.PostAsJsonAsync(new Uri(path, UriKind.Relative), body, TestContext.Current.CancellationToken);

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(code, body.RootElement.GetProperty("code").GetString());
    }

    [GeneratedRegex("href=\"(?<link>[^\"]+)\"", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Link();
}
