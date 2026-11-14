using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Self-service registration and email confirmation (PLAN F3b.2): the confirmation link starts from the configured public
/// URL, not the request's Host header; an existing address gets the same answer as a new one; outside Development the
/// host needs a real <see cref="IEmailSender"/>.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed partial class RegistrationTests(RegistrationTests.Fixture fixture) : IClassFixture<RegistrationTests.Fixture>
{
    private const string Password = "a long passphrase for tests";
    private const string OtherPassword = "another long passphrase";

    private readonly IdentityServerTestHost _host = fixture.Host;

    public sealed class Fixture : IdentityServerFixture
    {
        public CapturingEmailSender Emails { get; } = new();

        protected override void Configure(WebApplicationBuilder builder) =>
            builder.Services.AddSingleton<IEmailSender>(Emails);
    }

    [Fact]
    public async Task A_registered_user_confirms_the_email_and_signs_in()
    {
        var register = await RegisterAsync(_host.CreateClient(), "new@novacore.test");
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var link = await ConfirmationLinkAsync("new@novacore.test");
        Assert.StartsWith(IdentityServerTestHost.Authority + "/api/v1/auth/confirm-email?", link, StringComparison.Ordinal);

        var confirm = await _host.CreateClient().GetAsync(new Uri(new Uri(link).PathAndQuery, UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("new@novacore.test", Password);
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
    }

    [Fact]
    public async Task Registering_an_existing_address_looks_like_a_new_registration()
    {
        var client = _host.CreateClient();

        var first = await RegisterAsync(client, "twice@novacore.test");
        var second = await RegisterAsync(client, "twice@novacore.test");

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
        // The emails go out in the order queued: once a later one has gone, an earlier one would have too.
        await RegisterAsync(client, "after-twice@novacore.test");
        await fixture.Emails.WaitForAsync(email => string.Equals(email.To, "after-twice@novacore.test", StringComparison.Ordinal));
        Assert.Single(fixture.Emails.Sent, email => string.Equals(email.To, "twice@novacore.test", StringComparison.Ordinal));
    }

    [Fact]
    public async Task The_confirmation_link_ignores_the_Host_header()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/register")
        {
            Content = JsonContent.Create(new { email = "hostile@novacore.test", password = Password })
        };
        request.Headers.Host = "evil.example";

        Assert.Equal(HttpStatusCode.OK, (await _host.CreateClient().SendAsync(request)).StatusCode);

        Assert.StartsWith(IdentityServerTestHost.Authority + "/", await ConfirmationLinkAsync("hostile@novacore.test"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_invalid_registration_returns_the_validation_errors()
    {
        var response = await _host.CreateClient().PostAsJsonAsync("/api/v1/auth/register",
            new { email = "not-an-email", password = "too-short", displayName = new string('x', 129) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal(["DisplayName", "Email", "Password"], errors.EnumerateObject().Select(error => error.Name).Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    [Fact]
    public async Task A_bad_confirmation_token_is_rejected()
    {
        var user = await _host.CreateUserAsync("token@novacore.test", Password);

        var response = await _host.CreateClient().GetAsync(new Uri($"/api/v1/auth/confirm-email?userId={user.Id}&token=bm90LWEtdG9rZW4", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Resending_the_confirmation_reaches_an_unconfirmed_account_only()
    {
        var client = _host.CreateClient();
        await RegisterAsync(client, "resend@novacore.test");
        await _host.CreateUserAsync("resend-confirmed@novacore.test", Password);

        var unconfirmed = await ResendAsync(client, "resend@novacore.test");
        var confirmed = await ResendAsync(client, "resend-confirmed@novacore.test");
        var unknown = await ResendAsync(client, "resend-nobody@novacore.test");

        Assert.Equal(HttpStatusCode.OK, unconfirmed.StatusCode);
        var expected = await unconfirmed.Content.ReadAsStringAsync();
        Assert.Equal(expected, await confirmed.Content.ReadAsStringAsync());
        Assert.Equal(expected, await unknown.Content.ReadAsStringAsync());

        // The registration's link and the resent one. The emails go out in the order queued: once a later one has gone,
        // nothing queued before it is still pending.
        await RegisterAsync(client, "after-resend@novacore.test");
        await fixture.Emails.WaitForAsync(email => string.Equals(email.To, "after-resend@novacore.test", StringComparison.Ordinal));
        Assert.Equal(2, fixture.Emails.Sent.Count(email => string.Equals(email.To, "resend@novacore.test", StringComparison.Ordinal)));
        Assert.DoesNotContain(fixture.Emails.Sent, email => email.To is "resend-confirmed@novacore.test" or "resend-nobody@novacore.test");
    }

    [Fact]
    public async Task A_resent_link_confirms_the_email()
    {
        var client = _host.CreateClient();
        await RegisterAsync(client, "resend-link@novacore.test");
        await ResendAsync(client, "resend-link@novacore.test");

        var sent = await fixture.Emails.WaitForAsync(email => string.Equals(email.To, "resend-link@novacore.test", StringComparison.Ordinal), count: 2);
        Assert.Equal(2, sent.Count);
        var link = WebUtility.HtmlDecode(Href().Match(sent[1].HtmlBody).Groups["url"].Value);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(new Uri(new Uri(link).PathAndQuery, UriKind.Relative))).StatusCode);
    }

    // Account pre-hijacking (S-1, Sudhodanan & Paverd 2022): someone registers the address with a password of their own
    // first; the address's owner registers too and opens whatever confirmation link reaches the mailbox. No link may
    // confirm the account with the first password.
    [Fact]
    public async Task A_second_registration_stops_the_first_password_from_being_confirmed()
    {
        const string address = "pre-hijack@novacore.test";
        var client = _host.CreateClient();
        await RegisterAsync(client, address);
        await RegisterAsync(client, address, OtherPassword);
        await ResendAsync(client, address);

        foreach (var link in await LinksSentToAsync(address, "after-pre-hijack@novacore.test"))
        {
            if (link.AbsolutePath.EndsWith("/confirm-email", StringComparison.Ordinal))
                await client.GetAsync(new Uri(link.PathAndQuery, UriKind.Relative));
        }

        using var browser = new OidcFlow(_host);
        var (_, afterPassword) = await browser.BeginSignInAsync(address, Password);
        Assert.NotEqual(HttpStatusCode.Redirect, afterPassword.StatusCode);
    }

    [Fact]
    public async Task After_a_second_registration_the_mailbox_sets_the_password_and_confirms_the_account()
    {
        const string address = "claimed@novacore.test";
        var client = _host.CreateClient();
        await RegisterAsync(client, address);
        await RegisterAsync(client, address, OtherPassword);

        var link = Assert.Single(await LinksSentToAsync(address, "after-claimed@novacore.test"),
            sent => sent.AbsolutePath.EndsWith("/account/reset-password", StringComparison.Ordinal));
        using var browser = new OidcFlow(_host);
        var page = await browser.Browser.GetAsync(new Uri(link.PathAndQuery, UriKind.Relative));
        var set = await browser.SubmitFormAsync(page, "/account/reset-password", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Password"] = OtherPassword,
            ["ConfirmPassword"] = OtherPassword
        });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);

        using var owner = new OidcFlow(_host);
        Assert.False(string.IsNullOrEmpty((await owner.SignInAsync(address, OtherPassword)).AccessToken));
        using var first = new OidcFlow(_host);
        Assert.NotEqual(HttpStatusCode.Redirect, (await first.BeginSignInAsync(address, Password)).AfterPassword.StatusCode);
    }

    [Fact]
    public async Task Without_a_User_role_registration_still_sends_the_confirmation()
    {
        // Seeding off, as in Development by default: there are no roles at all.
        var emails = new CapturingEmailSender();
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { ["Identity:Seed:SeedOnStartup"] = "false" });
            builder.Services.AddSingleton<IEmailSender>(emails);
        });

        var response = await RegisterAsync(host.CreateClient(), "no-role@novacore.test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(await emails.WaitForAsync(email => string.Equals(email.To, "no-role@novacore.test", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Outside_Development_the_host_needs_an_email_sender()
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
            IdentityServerTestHost.StartAsync(Environments.Production, AddKeys));

        Assert.Contains($"No {nameof(IEmailSender)} is registered", exception.Message, StringComparison.Ordinal);

        await using var host = await IdentityServerTestHost.StartAsync(Environments.Production, builder =>
        {
            AddKeys(builder);
            builder.Services.AddSingleton<IEmailSender>(new CapturingEmailSender());
        });
        Assert.IsType<CapturingEmailSender>(host.Services.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public async Task In_Development_emails_go_to_the_log()
    {
        await using var host = await IdentityServerTestHost.StartAsync();

        Assert.Equal("LoggingEmailSender", host.Services.GetRequiredService<IEmailSender>().GetType().Name);
    }

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/v1/auth/register", new { email, password, displayName = "Test" });

    private static Task<HttpResponseMessage> ResendAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/v1/auth/resend-confirmation-email", new { email });

    private async Task<string> ConfirmationLinkAsync(string email)
    {
        var body = Assert.Single(await fixture.Emails.WaitForAsync(sent => string.Equals(sent.To, email, StringComparison.Ordinal))).HtmlBody;
        return WebUtility.HtmlDecode(Href().Match(body).Groups["url"].Value);
    }

    // Every link emailed to the address. The emails go out in the order queued: a registration of `later` queued after
    // them marks the end.
    private async Task<IReadOnlyList<Uri>> LinksSentToAsync(string email, string later)
    {
        await RegisterAsync(_host.CreateClient(), later);
        await fixture.Emails.WaitForAsync(sent => string.Equals(sent.To, later, StringComparison.Ordinal));
        return fixture.Emails.Sent
            .Where(sent => string.Equals(sent.To, email, StringComparison.Ordinal))
            .SelectMany(sent => Href().Matches(sent.HtmlBody))
            .Select(match => new Uri(WebUtility.HtmlDecode(match.Groups["url"].Value)))
            .ToList();
    }

    // The keys a production host brings: signing and encryption, and a Data Protection key store.
    private static void AddKeys(WebApplicationBuilder builder)
    {
        builder.Services.AddOpenIddict().AddServer(server => server.AddEphemeralSigningKey().AddEphemeralEncryptionKey());
        InMemoryKeyStore.AddTo(builder.Services);
    }

    [GeneratedRegex("href=\"(?<url>[^\"]+)\"", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Href();
}

/// <summary>An <see cref="IEmailSender"/> that keeps what it was asked to send.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => _sent;

    /// <summary>
    /// Waits until <paramref name="count"/> sent emails match. The provider sends them in the background, after the
    /// response (PLAN F6b.6a), one at a time in the order they were queued.
    /// </summary>
    public async Task<IReadOnlyList<SentEmail>> WaitForAsync(Func<SentEmail, bool> match, int count = 1)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var found = _sent.Where(match).ToList();
            if (found.Count >= count)
                return found;
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(toEmail, subject, htmlBody));
        return Task.CompletedTask;
    }
}

public sealed record SentEmail(string To, string Subject, string HtmlBody);
