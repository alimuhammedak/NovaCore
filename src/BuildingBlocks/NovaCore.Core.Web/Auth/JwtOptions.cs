namespace NovaCore.Core.Web.Auth;

/// <summary>Bound from the <c>Jwt</c> configuration section. Drives both token issuance and validation.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "novacore";
    public string Audience { get; set; } = "novacore.api";

    /// <summary>HMAC signing key. MUST be overridden in any non-dev environment (≥ 32 bytes / 256 bits).</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Short-lived by design (OWASP): a stolen access token expires fast; refresh handles longevity.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Refresh token absolute lifetime. Rotated on every use; reuse triggers family revocation.</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
