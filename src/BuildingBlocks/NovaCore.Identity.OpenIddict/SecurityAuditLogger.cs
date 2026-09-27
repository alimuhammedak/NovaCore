using Microsoft.Extensions.Logging;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Writes security events to the structured log under one "SECURITY" prefix, so they are easy to route to a SIEM.
/// Never logs secrets (passwords, tokens, codes); email addresses are masked.
/// </summary>
internal sealed class SecurityAuditLogger(ILogger<SecurityAuditLogger> logger)
{
    public void UserRegistered(Guid userId, string email)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogUserRegistered(userId, Mask(email));
    }

    public void EmailConfirmed(Guid userId) => logger.LogEmailConfirmed(userId);

    public void LoginSucceeded(Guid userId, string? ip) => logger.LogLoginSucceeded(userId, ip);

    public void LoginFailed(string? emailAttempted, string? ip, string reason)
    {
        // Mask allocates; skip it when the level is off (CA1873).
        if (logger.IsEnabled(LogLevel.Warning))
            logger.LogLoginFailed(Mask(emailAttempted), ip, reason);
    }

    public void AccountLockedOut(Guid userId, string? ip) => logger.LogAccountLockedOut(userId, ip);

    public void TwoFactorChallenged(Guid userId) => logger.LogTwoFactorChallenged(userId);

    public void TwoFactorEnabled(Guid userId) => logger.LogTwoFactorEnabled(userId);

    public void TwoFactorDisabled(Guid userId) => logger.LogTwoFactorDisabled(userId);

    public void TokenRefreshed(Guid userId, string? ip) => logger.LogTokenRefreshed(userId, ip);

    public void SignedOut(Guid userId) => logger.LogSignedOut(userId);

    // Light masking: the first character and the domain, so logs don't store full addresses.
    private static string Mask(string? email)
    {
        var at = email?.IndexOf('@', StringComparison.Ordinal) ?? -1;
        return at <= 1 ? "***" : $"{email![0]}***{email[at..]}";
    }
}

// Extension methods rather than instance [LoggerMessage] methods: CA1873 accepts an IsEnabled guard only when the guard
// and the log call share the same logger receiver (docs/plan/RESEARCH.md R-047).
internal static partial class SecurityAuditLog
{
    [LoggerMessage(EventId = 16, Level = LogLevel.Information, Message = "SECURITY user_registered User={UserId} Email={Email}")]
    public static partial void LogUserRegistered(this ILogger logger, Guid userId, string email);

    [LoggerMessage(EventId = 17, Level = LogLevel.Information, Message = "SECURITY email_confirmed User={UserId}")]
    public static partial void LogEmailConfirmed(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "SECURITY login_succeeded User={UserId} Ip={Ip}")]
    public static partial void LogLoginSucceeded(this ILogger logger, Guid userId, string? ip);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "SECURITY login_failed Email={Email} Ip={Ip} Reason={Reason}")]
    public static partial void LogLoginFailed(this ILogger logger, string email, string? ip, string reason);

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "SECURITY account_locked_out User={UserId} Ip={Ip}")]
    public static partial void LogAccountLockedOut(this ILogger logger, Guid userId, string? ip);

    [LoggerMessage(EventId = 18, Level = LogLevel.Information, Message = "SECURITY two_factor_challenged User={UserId}")]
    public static partial void LogTwoFactorChallenged(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 19, Level = LogLevel.Information, Message = "SECURITY two_factor_enabled User={UserId}")]
    public static partial void LogTwoFactorEnabled(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 20, Level = LogLevel.Warning, Message = "SECURITY two_factor_disabled User={UserId}")]
    public static partial void LogTwoFactorDisabled(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "SECURITY token_refreshed User={UserId} Ip={Ip}")]
    public static partial void LogTokenRefreshed(this ILogger logger, Guid userId, string? ip);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "SECURITY signed_out User={UserId}")]
    public static partial void LogSignedOut(this ILogger logger, Guid userId);
}
