using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Sends the provider's transactional emails, such as the email confirmation link. Outside Development the host must
/// register one (SMTP, a mail API, ...): the host does not start without it.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends an HTML email.</summary>
    /// <param name="toEmail">The recipient's address.</param>
    /// <param name="subject">The subject line.</param>
    /// <param name="htmlBody">The HTML body. It may contain a confirmation link, which is a credential.</param>
    /// <param name="cancellationToken">Cancels the send.</param>
    /// <returns>A task that completes when the email is handed over.</returns>
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

/// <summary>
/// Development only: writes the email, confirmation link included, to the log instead of sending it, so the flow works
/// locally without a mail server.
/// </summary>
internal sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        LogEmail(toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 31, Level = LogLevel.Information, Message = "DEV EMAIL → {To} | {Subject}\n{Body}")]
    private partial void LogEmail(string to, string subject, string body);
}

/// <summary>
/// Stops the host outside Development when no <see cref="IEmailSender"/> is registered, instead of failing the first
/// registration. Runs once the container is built, so it also sees a sender the host registered after the add-on.
/// </summary>
internal sealed class EmailSenderStartupCheck(IHostEnvironment environment, IServiceProviderIsService registrations)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !registrations.IsService(typeof(IEmailSender)))
            throw new InvalidOperationException(
                $"No {nameof(IEmailSender)} is registered for the '{environment.EnvironmentName}' environment. "
                + $"{nameof(LoggingEmailSender)} is Development-only because it writes emails, confirmation links included, "
                + $"to the log. Register a real {nameof(IEmailSender)} (SMTP, a mail API, ...) in the host.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
