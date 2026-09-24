namespace NovaCore.Modules.Identity.Domain;

/// <summary>
/// A refresh token, stored <strong>hashed</strong> (never in plaintext). Tokens belong to a "family";
/// rotation issues a new token in the same family and revokes the old one. Presenting an already-rotated
/// token (reuse) revokes the whole family — the OWASP-recommended theft-detection mechanism.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash of the raw token. The raw value is returned to the client exactly once.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Lineage id shared by every token produced through rotation from one login.</summary>
    public Guid Family { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedByIp { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? ReasonRevoked { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsActive => RevokedAtUtc is null && !IsExpired;
}
