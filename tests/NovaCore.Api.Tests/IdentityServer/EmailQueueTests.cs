using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// PLAN F6b.6a: the account pages answer before the email goes out. A response that waited for the mail server would
/// take longer exactly when the address has an account (registration of a new address, a reset for a confirmed one,
/// a resend for an unconfirmed one), so its timing would reveal the account (OWASP Forgot Password Cheat Sheet).
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class EmailQueueTests
{
    private const string Password = "a long passphrase for tests";

    // Generous: a response that waits for the held sender never comes, one that doesn't comes in milliseconds.
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Registration_answers_before_the_email_is_sent()
    {
        var sender = new HeldEmailSender();
        await using var host = await StartAsync(sender);

        var response = await host.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/register", new { email = "queued-register@novacore.test", password = Password })
            .WaitAsync(ResponseTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Release();
        await sender.Delivered.WaitForAsync(email => email.To == "queued-register@novacore.test");
    }

    [Fact]
    public async Task A_password_reset_request_answers_before_the_email_is_sent()
    {
        var sender = new HeldEmailSender();
        await using var host = await StartAsync(sender);
        await host.CreateUserAsync("queued-reset@novacore.test", Password);
        using var browser = new OidcFlow(host);
        var page = await browser.Browser.GetAsync(new Uri("/account/forgot-password", UriKind.Relative), TestContext.Current.CancellationToken);

        var response = await browser.SubmitFormAsync(page, "/account/forgot-password", new() { ["Email"] = "queued-reset@novacore.test" })
            .WaitAsync(ResponseTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Release();
        await sender.Delivered.WaitForAsync(email => email.To == "queued-reset@novacore.test");
    }

    [Fact]
    public async Task Resending_the_confirmation_answers_before_the_email_is_sent()
    {
        var sender = new HeldEmailSender();
        await using var host = await StartAsync(sender);
        var client = host.CreateClient();
        await client
            .PostAsJsonAsync("/api/v1/auth/register", new { email = "queued-resend@novacore.test", password = Password })
            .WaitAsync(ResponseTimeout, TestContext.Current.CancellationToken);

        var response = await client
            .PostAsJsonAsync("/api/v1/auth/resend-confirmation-email", new { email = "queued-resend@novacore.test" })
            .WaitAsync(ResponseTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        sender.Release();
        await sender.Delivered.WaitForAsync(email => email.To == "queued-resend@novacore.test", count: 2);
    }

    [Fact]
    public async Task A_failed_send_is_logged_without_the_address_and_the_next_email_still_goes()
    {
        var sender = new FailingOnceEmailSender();
        using var log = new LogCapture();
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
        {
            builder.Services.AddSingleton<IEmailSender>(sender);
            builder.Logging.AddProvider(log);
        });
        var client = host.CreateClient();

        var first = await client.PostAsJsonAsync("/api/v1/auth/register", new { email = "fails-first@novacore.test", password = Password });
        var second = await client.PostAsJsonAsync("/api/v1/auth/register", new { email = "goes-second@novacore.test", password = Password });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        await sender.Delivered.WaitForAsync(email => email.To == "goes-second@novacore.test");
        var failure = Assert.Single(log.Messages, message => message.StartsWith("Sending an email failed", StringComparison.Ordinal));
        Assert.DoesNotContain("fails-first", failure, StringComparison.Ordinal);
    }

    // A queue that waited for room when full would hold up exactly the requests that send an email, and its timing would
    // reveal accounts again: with the mail server stuck, the email that doesn't fit is dropped and logged instead.
    [Fact]
    public async Task A_full_queue_drops_the_email_instead_of_holding_the_response()
    {
        var sender = new HeldEmailSender();
        using var log = new LogCapture();
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
        {
            builder.Services.AddSingleton<IEmailSender>(sender);
            builder.Logging.AddProvider(log);
        });
        await host.CreateUserAsync("queue-full@novacore.test", Password);
        using var browser = new OidcFlow(host);
        var page = await browser.Browser.GetAsync(new Uri("/account/forgot-password", UriKind.Relative), TestContext.Current.CancellationToken);

        // One email held by the sender, a thousand waiting: the queue is full.
        for (var request = 0; request < 1001; request++)
            await browser.SubmitFormAsync(page, "/account/forgot-password", new() { ["Email"] = "queue-full@novacore.test" });

        var response = await browser.SubmitFormAsync(page, "/account/forgot-password", new() { ["Email"] = "queue-full@novacore.test" })
            .WaitAsync(ResponseTimeout, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(log.Messages, message => message.StartsWith("The email queue is full", StringComparison.Ordinal));
        sender.Release();
    }

    private static Task<IdentityServerTestHost> StartAsync(IEmailSender sender) =>
        IdentityServerTestHost.StartAsync(configure: builder => builder.Services.AddSingleton(sender));

    // A mail server that takes as long as the test wants.
    private sealed class HeldEmailSender : IEmailSender
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CapturingEmailSender Delivered { get; } = new();

        public void Release() => _gate.TrySetResult();

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            await _gate.Task.WaitAsync(cancellationToken);
            await Delivered.SendAsync(toEmail, subject, htmlBody, cancellationToken);
        }
    }

    private sealed class FailingOnceEmailSender : IEmailSender
    {
        private int _calls;

        public CapturingEmailSender Delivered { get; } = new();

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default) =>
            Interlocked.Increment(ref _calls) == 1
                ? throw new InvalidOperationException("The mail server is down.")
                : Delivered.SendAsync(toEmail, subject, htmlBody, cancellationToken);
    }
}
