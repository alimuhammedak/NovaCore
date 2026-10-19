using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
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

        var link = ConfirmationLink("new@novacore.test");
        Assert.StartsWith(IdentityServerTestHost.Authority + "/api/v1/auth/confirm-email?", link, StringComparison.Ordinal);

        var confirm = await _host.CreateClient().GetAsync(new Uri(link).PathAndQuery);
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
        Assert.Single(fixture.Emails.Sent, email => email.To == "twice@novacore.test");
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

        Assert.StartsWith(IdentityServerTestHost.Authority + "/", ConfirmationLink("hostile@novacore.test"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_invalid_registration_returns_the_validation_errors()
    {
        var response = await _host.CreateClient().PostAsJsonAsync("/api/v1/auth/register",
            new { email = "not-an-email", password = "too-short", displayName = new string('x', 129) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal(["DisplayName", "Email", "Password"], errors.EnumerateObject().Select(error => error.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task A_bad_confirmation_token_is_rejected()
    {
        var user = await _host.CreateUserAsync("token@novacore.test", Password);

        var response = await _host.CreateClient().GetAsync($"/api/v1/auth/confirm-email?userId={user.Id}&token=bm90LWEtdG9rZW4");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password, displayName = "Test" });

    private string ConfirmationLink(string email)
    {
        var body = Assert.Single(fixture.Emails.Sent, sent => sent.To == email).HtmlBody;
        return WebUtility.HtmlDecode(Href().Match(body).Groups["url"].Value);
    }

    // The keys a production host brings: signing and encryption, and a Data Protection key store.
    private static void AddKeys(WebApplicationBuilder builder)
    {
        builder.Services.AddOpenIddict().AddServer(server => server.AddEphemeralSigningKey().AddEphemeralEncryptionKey());
        InMemoryKeyStore.AddTo(builder.Services);
    }

    [GeneratedRegex("href=\"(?<url>[^\"]+)\"")]
    private static partial Regex Href();
}

/// <summary>An <see cref="IEmailSender"/> that keeps what it was asked to send.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => _sent;

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(toEmail, subject, htmlBody));
        return Task.CompletedTask;
    }
}

public sealed record SentEmail(string To, string Subject, string HtmlBody);
