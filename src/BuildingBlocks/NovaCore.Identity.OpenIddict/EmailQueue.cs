using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Sends the provider's emails off the request: an endpoint queues the email and answers at once, and this service hands
/// it to the host's <see cref="IEmailSender"/> in the background, one at a time in the order queued. A response that
/// waited for the mail server would take longer exactly when the address has an account (a new registration, a reset for
/// a confirmed account, a resend for an unconfirmed one), so its timing would reveal the account. OWASP's Forgot
/// Password Cheat Sheet names asynchronous calls for it; ASVS 6.3.8 (docs/plan/RESEARCH.md R-105).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Microsoft's queued background task pattern: a bounded channel whose writer waits for room when it is full, so a
/// burst slows down instead of growing the queue without limit.</item>
/// <item>The queue lives in memory: an email still in it when the process stops is lost, and the user asks for the link
/// again. A failed send is logged and the next email goes on.</item>
/// <item>Neither the address nor the body, which holds a credential link, reaches the log.</item>
/// </list>
/// </remarks>
internal sealed partial class EmailQueue(IServiceScopeFactory scopeFactory, ILogger<EmailQueue> logger) : BackgroundService
{
    // Room for a burst far above what the "strict" rate limit lets one client send.
    private const int Capacity = 1000;

    private readonly Channel<QueuedEmail> _emails = Channel.CreateBounded<QueuedEmail>(
        new BoundedChannelOptions(Capacity) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });

    /// <summary>Queues an email; completes once it is queued, not sent.</summary>
    public ValueTask EnqueueAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken) =>
        _emails.Writer.WriteAsync(new QueuedEmail(toEmail, subject, htmlBody), cancellationToken);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in _emails.Reader.ReadAllAsync(stoppingToken))
            await SendAsync(email, stoppingToken);
    }

    private async Task SendAsync(QueuedEmail email, CancellationToken cancellationToken)
    {
        try
        {
            // A scope per email: the host's sender may be scoped.
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IEmailSender>()
                .SendAsync(email.ToEmail, email.Subject, email.HtmlBody, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // The loop's boundary: an unreachable mail server must not stop the host or the emails after this one. Only
            // the host's own stop gets through.
            LogSendFailed(exception, email.Subject);
        }
    }

    private sealed record QueuedEmail(string ToEmail, string Subject, string HtmlBody);

    [LoggerMessage(EventId = 32, Level = LogLevel.Error, Message = "Sending an email failed: {Subject}")]
    private partial void LogSendFailed(Exception exception, string subject);
}
