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
    public void UserRegistered(Guid userId, string email) =>
        logger.LogInformation("SECURITY user_registered User={UserId} Email={Email}", userId, Mask(email));

    public void EmailConfirmed(Guid userId) =>
        logger.LogInformation("SECURITY email_confirmed User={UserId}", userId);

    public void LoginSucceeded(Guid userId, string? ip) =>
        logger.LogInformation("SECURITY login_succeeded User={UserId} Ip={Ip}", userId, ip);

    public void LoginFailed(string emailAttempted, string? ip, string reason) =>
        logger.LogWarning("SECURITY login_failed Email={Email} Ip={Ip} Reason={Reason}", Mask(emailAttempted), ip, reason);

    public void AccountLockedOut(Guid userId, string? ip) =>
        logger.LogWarning("SECURITY account_locked_out User={UserId} Ip={Ip}", userId, ip);

    public void TwoFactorChallenged(Guid userId) =>
        logger.LogInformation("SECURITY two_factor_challenged User={UserId}", userId);

    public void TwoFactorEnabled(Guid userId) =>
        logger.LogInformation("SECURITY two_factor_enabled User={UserId}", userId);

    public void TwoFactorDisabled(Guid userId) =>
        logger.LogWarning("SECURITY two_factor_disabled User={UserId}", userId);

    public void TokenRefreshed(Guid userId, string? ip) =>
        logger.LogInformation("SECURITY token_refreshed User={UserId} Ip={Ip}", userId, ip);

    public void RefreshTokenReuseDetected(Guid userId, Guid family, string? ip) =>
        logger.LogError("SECURITY refresh_token_reuse_detected User={UserId} Family={Family} Ip={Ip} — family revoked", userId, family, ip);

    public void LoggedOut(Guid userId, bool allSessions) =>
        logger.LogInformation("SECURITY logged_out User={UserId} AllSessions={All}", userId, allSessions);

    public void PasswordChanged(Guid userId) =>
        logger.LogInformation("SECURITY password_changed User={UserId}", userId);

    public void RoleAssigned(Guid userId, string role, string? byUserId) =>
        logger.LogInformation("SECURITY role_assigned User={UserId} Role={Role} By={By}", userId, role, byUserId);

    public void RoleRemoved(Guid userId, string role, string? byUserId) =>
        logger.LogInformation("SECURITY role_removed User={UserId} Role={Role} By={By}", userId, role, byUserId);

    // Light masking so audit logs don't store full addresses verbatim.
    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
            return "***";
        return $"{email[0]}***{email[at..]}";
    }
}
