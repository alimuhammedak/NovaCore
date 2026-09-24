using Microsoft.Extensions.Logging;

namespace NovaCore.Modules.Identity.Services;

/// <summary>Sends transactional emails (confirmation, password reset). Swap for SMTP/SendGrid in prod.</summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

/// <summary>
/// Development email sender: writes the message to the log instead of sending it, so confirmation links
/// are visible during local development without an SMTP server. NOT for production.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("DEV EMAIL → {To} | {Subject}\n{Body}", toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}
