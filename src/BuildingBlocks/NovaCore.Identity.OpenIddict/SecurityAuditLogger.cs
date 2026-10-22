using Microsoft.Extensions.Logging;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Writes security events to the structured log under one "SECURITY" prefix, so they are easy to route to a SIEM.
/// Never logs secrets (passwords, tokens, codes) or email addresses, not even masked ones (Y-35): an event names the
/// account by its id, and a failed sign-in for an unknown address names none.
/// </summary>
internal sealed class SecurityAuditLogger(ILogger<SecurityAuditLogger> logger)
{
    public void UserRegistered(Guid userId) => logger.LogUserRegistered(userId);

    public void EmailConfirmed(Guid userId) => logger.LogEmailConfirmed(userId);

    public void LoginSucceeded(Guid userId, string? ip) => logger.LogLoginSucceeded(userId, ip);

    public void LoginFailed(Guid? userId, string? ip, string reason) => logger.LogLoginFailed(userId, ip, reason);

    public void AccountLockedOut(Guid userId, string? ip) => logger.LogAccountLockedOut(userId, ip);

    public void TwoFactorChallenged(Guid userId) => logger.LogTwoFactorChallenged(userId);

    public void TwoFactorEnabled(Guid userId) => logger.LogTwoFactorEnabled(userId);

    public void TwoFactorDisabled(Guid userId) => logger.LogTwoFactorDisabled(userId);

    public void RecoveryCodeUsed(Guid userId) => logger.LogRecoveryCodeUsed(userId);

    public void RecoveryCodesRenewed(Guid userId) => logger.LogRecoveryCodesRenewed(userId);

    public void TokenRefreshed(Guid userId, string? ip) => logger.LogTokenRefreshed(userId, ip);

    public void SignedOut(Guid userId) => logger.LogSignedOut(userId);

    public void SessionsRevoked(Guid userId, string reason) => logger.LogSessionsRevoked(userId, reason);

    public void PasswordChanged(Guid userId) => logger.LogPasswordChanged(userId);

    public void PasswordResetRequested(Guid userId) => logger.LogPasswordResetRequested(userId);

    public void PasswordReset(Guid userId) => logger.LogPasswordReset(userId);

    public void RoleAssigned(Guid userId, string role, string? byUserId) => logger.LogRoleAssigned(userId, role, byUserId);

    public void RoleRemoved(Guid userId, string role, string? byUserId) => logger.LogRoleRemoved(userId, role, byUserId);
}

// Extension methods rather than instance [LoggerMessage] methods: CA1873 accepts an IsEnabled guard only when the guard
// and the log call share the same logger receiver (docs/plan/RESEARCH.md R-047). No event computes an argument since
// the email masking left (F6b.6d), so none is guarded now.
internal static partial class SecurityAuditLog
{
    [LoggerMessage(EventId = 16, Level = LogLevel.Information, Message = "SECURITY user_registered User={UserId}")]
    public static partial void LogUserRegistered(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 17, Level = LogLevel.Information, Message = "SECURITY email_confirmed User={UserId}")]
    public static partial void LogEmailConfirmed(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "SECURITY login_succeeded User={UserId} Ip={Ip}")]
    public static partial void LogLoginSucceeded(this ILogger logger, Guid userId, string? ip);

    // No user for an unknown address: the address itself is personal data, and the IP and reason say enough.
    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "SECURITY login_failed User={UserId} Ip={Ip} Reason={Reason}")]
    public static partial void LogLoginFailed(this ILogger logger, Guid? userId, string? ip, string reason);

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "SECURITY account_locked_out User={UserId} Ip={Ip}")]
    public static partial void LogAccountLockedOut(this ILogger logger, Guid userId, string? ip);

    [LoggerMessage(EventId = 18, Level = LogLevel.Information, Message = "SECURITY two_factor_challenged User={UserId}")]
    public static partial void LogTwoFactorChallenged(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 19, Level = LogLevel.Information, Message = "SECURITY two_factor_enabled User={UserId}")]
    public static partial void LogTwoFactorEnabled(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 20, Level = LogLevel.Warning, Message = "SECURITY two_factor_disabled User={UserId}")]
    public static partial void LogTwoFactorDisabled(this ILogger logger, Guid userId);

    // Warning: a used recovery code can mean a lost authenticator, or a stolen code.
    [LoggerMessage(EventId = 27, Level = LogLevel.Warning, Message = "SECURITY recovery_code_used User={UserId}")]
    public static partial void LogRecoveryCodeUsed(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 28, Level = LogLevel.Information, Message = "SECURITY recovery_codes_renewed User={UserId}")]
    public static partial void LogRecoveryCodesRenewed(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information, Message = "SECURITY token_refreshed User={UserId} Ip={Ip}")]
    public static partial void LogTokenRefreshed(this ILogger logger, Guid userId, string? ip);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "SECURITY signed_out User={UserId}")]
    public static partial void LogSignedOut(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 21, Level = LogLevel.Information, Message = "SECURITY password_changed User={UserId}")]
    public static partial void LogPasswordChanged(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 25, Level = LogLevel.Information, Message = "SECURITY password_reset_requested User={UserId}")]
    public static partial void LogPasswordResetRequested(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 26, Level = LogLevel.Information, Message = "SECURITY password_reset User={UserId}")]
    public static partial void LogPasswordReset(this ILogger logger, Guid userId);

    [LoggerMessage(EventId = 22, Level = LogLevel.Information, Message = "SECURITY sessions_revoked User={UserId} Reason={Reason}")]
    public static partial void LogSessionsRevoked(this ILogger logger, Guid userId, string reason);

    [LoggerMessage(EventId = 23, Level = LogLevel.Information, Message = "SECURITY role_assigned User={UserId} Role={Role} By={By}")]
    public static partial void LogRoleAssigned(this ILogger logger, Guid userId, string role, string? by);

    [LoggerMessage(EventId = 24, Level = LogLevel.Information, Message = "SECURITY role_removed User={UserId} Role={Role} By={By}")]
    public static partial void LogRoleRemoved(this ILogger logger, Guid userId, string role, string? by);
}
