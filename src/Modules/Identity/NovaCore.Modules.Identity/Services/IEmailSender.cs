using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
public sealed partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
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
/// Fails startup outside Development when no <see cref="IEmailSender"/> is registered, instead of letting the first
/// registration request fail. Runs after the container is built, so it also sees a sender the host registered after
/// the modules.
/// </summary>
internal sealed class EmailSenderStartupCheck(IHostEnvironment environment, IServiceProviderIsService registrations)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !registrations.IsService(typeof(IEmailSender)))
            throw new InvalidOperationException(
                $"No {nameof(IEmailSender)} is registered for the '{environment.EnvironmentName}' environment. " +
                $"{nameof(LoggingEmailSender)} is Development-only because it writes emails, including confirmation " +
                $"tokens, to the log. Register a real {nameof(IEmailSender)} (SMTP, SendGrid, ...) in the host.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
