using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Auth;
using NovaCore.AspNetCore.Identity;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Infrastructure;

namespace NovaCore.Modules.Identity.Services;

/// <summary>
/// Issues and rotates the access/refresh token pair. Access tokens are short-lived JWTs carrying the
/// user's roles + permissions as claims. Refresh tokens are opaque, stored hashed, rotated on every
/// use, and a replayed (already-rotated) token revokes its entire family — OWASP reuse detection.
/// </summary>
public interface IAuthTokenService
{
    Task<AuthTokens> IssueAsync(ApplicationUser user, string? ip, CancellationToken cancellationToken = default);
    Task<Result<AuthTokens>> RefreshAsync(string rawRefreshToken, string? ip, CancellationToken cancellationToken = default);
    Task RevokeAsync(string rawRefreshToken, string? ip, CancellationToken cancellationToken = default);
    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class AuthTokenService(
    IdentityDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ITokenService accessTokens,
    ISecurityAuditLogger audit,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider) : IAuthTokenService
{
    public async Task<AuthTokens> IssueAsync(ApplicationUser user, string? ip, CancellationToken cancellationToken = default)
    {
        var (raw, entity) = CreateRefreshToken(user.Id, ip, family: null, timeProvider.GetUtcNow());
        dbContext.RefreshTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildAsync(user, raw, entity.ExpiresAtUtc);
    }

    public async Task<Result<AuthTokens>> RefreshAsync(string rawRefreshToken, string? ip, CancellationToken cancellationToken = default)
    {
        var hash = Hash(rawRefreshToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null)
            return Error.Unauthorized("auth.invalid_refresh_token", "Invalid refresh token.");

        // Replay of an already-rotated/revoked token ⇒ theft. Kill the whole family.
        if (token.RevokedAtUtc is not null)
        {
            await RevokeFamilyAsync(token.Family, ip, "reuse-detected", cancellationToken);
            audit.RefreshTokenReuseDetected(token.UserId, token.Family, ip);
            return Error.Unauthorized("auth.refresh_token_reuse", "Refresh token reuse detected; all sessions were revoked.");
        }

        var now = timeProvider.GetUtcNow();
        if (token.IsExpired(now))
            return Error.Unauthorized("auth.refresh_token_expired", "Refresh token has expired.");

        var user = await userManager.FindByIdAsync(token.UserId.ToString());
        if (user is null)
            return Error.Unauthorized("auth.invalid_refresh_token", "Invalid refresh token.");

        // Rotate: revoke current, mint a successor in the same family.
        var (raw, successor) = CreateRefreshToken(user.Id, ip, token.Family, now);
        token.RevokedAtUtc = now;
        token.RevokedByIp = ip;
        token.ReasonRevoked = "rotated";
        token.ReplacedByTokenHash = successor.TokenHash;
        dbContext.RefreshTokens.Add(successor);
        await dbContext.SaveChangesAsync(cancellationToken);

        audit.TokenRefreshed(user.Id, ip);
        return await BuildAsync(user, raw, successor.ExpiresAtUtc);
    }

    public async Task RevokeAsync(string rawRefreshToken, string? ip, CancellationToken cancellationToken = default)
    {
        var hash = Hash(rawRefreshToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (token is null)
            return;

        // Revoke the whole family so any in-flight rotation is also invalidated (clean logout).
        await RevokeFamilyAsync(token.Family, ip, "logout", cancellationToken);
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(set => set
                .SetProperty(t => t.RevokedAtUtc, now)
                .SetProperty(t => t.ReasonRevoked, "logout-all"), cancellationToken);
    }

    private async Task RevokeFamilyAsync(Guid family, string? ip, string reason, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await dbContext.RefreshTokens
            .Where(t => t.Family == family && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(set => set
                .SetProperty(t => t.RevokedAtUtc, now)
                .SetProperty(t => t.RevokedByIp, ip)
                .SetProperty(t => t.ReasonRevoked, reason), cancellationToken);
    }

    private (string Raw, RefreshToken Entity) CreateRefreshToken(Guid userId, string? ip, Guid? family, DateTimeOffset now)
    {
        var raw = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32)); // 256-bit, URL-safe
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(raw),
            Family = family ?? Guid.CreateVersion7(),
            CreatedAtUtc = now,
            CreatedByIp = ip,
            ExpiresAtUtc = now.AddDays(jwtOptions.Value.RefreshTokenDays)
        };
        return (raw, entity);
    }

    private async Task<AuthTokens> BuildAsync(ApplicationUser user, string rawRefreshToken, DateTimeOffset refreshExpiry)
    {
        var claims = await BuildClaimsAsync(user);
        var access = accessTokens.CreateAccessToken(claims);
        return new AuthTokens(access.Token, "Bearer", access.ExpiresInSeconds, rawRefreshToken, refreshExpiry);
    }

    private async Task<IReadOnlyCollection<Claim>> BuildClaimsAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(NovaClaims.Subject, user.Id.ToString()),
            new(NovaClaims.Name, user.UserName ?? user.Email ?? user.Id.ToString())
        };

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(NovaClaims.Email, user.Email));

        var permissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var roleName in await userManager.GetRolesAsync(user))
        {
            claims.Add(new Claim(NovaClaims.Role, roleName));

            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
                continue;

            foreach (var claim in await roleManager.GetClaimsAsync(role))
                if (claim.Type == NovaClaims.Permission)
                    permissions.Add(claim.Value);
        }

        // Direct, user-specific permission grants augment role permissions.
        foreach (var claim in await userManager.GetClaimsAsync(user))
            if (claim.Type == NovaClaims.Permission)
                permissions.Add(claim.Value);

        claims.AddRange(permissions.Select(p => new Claim(NovaClaims.Permission, p)));
        return claims;
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
