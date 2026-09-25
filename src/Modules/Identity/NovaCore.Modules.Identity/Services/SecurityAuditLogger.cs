using Microsoft.Extensions.Logging;

namespace NovaCore.Modules.Identity.Services;

/// <summary>
/// Records security-relevant events to the structured log (Serilog → Console/Seq). Centralising them
/// here makes it trivial to also ship them to a SIEM later. Never logs secrets (passwords, raw tokens).
/// </summary>
public interface ISecurityAuditLogger
{
    void UserRegistered(Guid userId, string email);
    void EmailConfirmed(Guid userId);
    void LoginSucceeded(Guid userId, string? ip);
    void LoginFailed(string emailAttempted, string? ip, string reason);
    void AccountLockedOut(Guid userId, string? ip);
    void TwoFactorChallenged(Guid userId);
    void TwoFactorEnabled(Guid userId);
    void TwoFactorDisabled(Guid userId);
    void TokenRefreshed(Guid userId, string? ip);
    void RefreshTokenReuseDetected(Guid userId, Guid family, string? ip);
    void LoggedOut(Guid userId, bool allSessions);
    void PasswordChanged(Guid userId);
    void RoleAssigned(Guid userId, string role, string? byUserId);
    void RoleRemoved(Guid userId, string role, string? byUserId);
}

public sealed class SecurityAuditLogger(ILogger<SecurityAuditLogger> logger) : ISecurityAuditLogger
{
    public void UserRegistered(Guid userId, string email)
    {
        // Mask allocates; skip it when Information is off (CA1873).
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogUserRegistered(userId, Mask(email));
    }

    public void EmailConfirmed(Guid userId) => logger.LogEmailConfirmed(userId);

    public void LoginSucceeded(Guid userId, string? ip) => logger.LogLoginSucceeded(userId, ip);

    public void LoginFailed(string emailAttempted, string? ip, string reason) =>
        logger.LogLoginFailed(Mask(emailAttempted), ip, reason);

    public void AccountLockedOut(Guid userId, string? ip) => logger.LogAccountLockedOut(userId, ip);

    public void TwoFactorChallenged(Guid userId) => logger.LogTwoFactorChallenged(userId);

    public void TwoFactorEnabled(Guid userId) => logger.LogTwoFactorEnabled(userId);

    public void TwoFactorDisabled(Guid userId) => logger.LogTwoFactorDisabled(userId);

    public void TokenRefreshed(Guid userId, string? ip) => logger.LogTokenRefreshed(userId, ip);

    public void RefreshTokenReuseDetected(Guid userId, Guid family, string? ip) =>
        logger.LogRefreshTokenReuseDetected(userId, family, ip);

    public void LoggedOut(Guid userId, bool allSessions) => logger.LogLoggedOut(userId, allSessions);

    public void PasswordChanged(Guid userId) => logger.LogPasswordChanged(userId);

    public void RoleAssigned(Guid userId, string role, string? byUserId) => logger.LogRoleAssigned(userId, role, byUserId);

    public void RoleRemoved(Guid userId, string role, string? byUserId) => logger.LogRoleRemoved(userId, role, byUserId);

    // Light masking so audit logs don't store full addresses verbatim.
    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
            return "***";
        return $"{email[0]}***{email[at..]}";
    }
}

// Extension methods rather than instance [LoggerMessage] methods: CA1873 accepts an IsEnabled guard only when the guard
// and the log call share the same logger receiver (docs/plan/RESEARCH.md R-047).
internal static partial class SecurityAuditLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "SECURITY user_registered User={UserId} Email={Email}")]
    public static partial void LogUserRegistered(this ILogger logger, Guid userId, string email);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "SECURITY email_confirmed User={UserId}")]
    public static partial void LogEmailConfirmed(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "SECURITY login_succeeded User={UserId} Ip={Ip}")]
    public static partial void LogLoginSucceeded(this ILogger logger, Guid userId, string? ip);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "SECURITY login_failed Email={Email} Ip={Ip} Reason={Reason}")]
    public static partial void LogLoginFailed(this ILogger logger, string email, string? ip, string reason);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "SECURITY account_locked_out User={UserId} Ip={Ip}")]
    public static partial void LogAccountLockedOut(this ILogger logger, Guid userId, string? ip);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "SECURITY two_factor_challenged User={UserId}")]
    public static partial void LogTwoFactorChallenged(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "SECURITY two_factor_enabled User={UserId}")]
    public static partial void LogTwoFactorEnabled(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "SECURITY two_factor_disabled User={UserId}")]
    public static partial void LogTwoFactorDisabled(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "SECURITY token_refreshed User={UserId} Ip={Ip}")]
    public static partial void LogTokenRefreshed(this ILogger logger, Guid userId, string? ip);

    [LoggerMessage(EventId = 10, Level = LogLevel.Error, Message = "SECURITY refresh_token_reuse_detected User={UserId} Family={Family} Ip={Ip} — family revoked")]
    public static partial void LogRefreshTokenReuseDetected(this ILogger logger, Guid userId, Guid family, string? ip);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "SECURITY logged_out User={UserId} AllSessions={All}")]
    public static partial void LogLoggedOut(this ILogger logger, Guid userId, bool all);

    [LoggerMessage(EventId = 12, Level = LogLevel.Information, Message = "SECURITY password_changed User={UserId}")]
    public static partial void LogPasswordChanged(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = "SECURITY role_assigned User={UserId} Role={Role} By={By}")]
    public static partial void LogRoleAssigned(this ILogger logger, Guid userId, string role, string? by);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "SECURITY role_removed User={UserId} Role={Role} By={By}")]
    public static partial void LogRoleRemoved(this ILogger logger, Guid userId, string role, string? by);
}
