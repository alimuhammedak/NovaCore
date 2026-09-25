using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace NovaCore.AspNetCore.Auth;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section. Drives both token issuance and validation, and is validated when the
/// host starts.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Jwt";

    /// <summary>The <c>iss</c> claim written into tokens and required when validating them.</summary>
    [Required]
    public string Issuer { get; set; } = "novacore";

    /// <summary>The <c>aud</c> claim written into tokens and required when validating them.</summary>
    [Required]
    public string Audience { get; set; } = "novacore.api";

    /// <summary>
    /// HMAC signing key, at least 32 bytes (256 bits). Required outside Development; in Development a throwaway key is
    /// used when none is configured.
    /// </summary>
    [Required(ErrorMessage = "Jwt:SigningKey is required outside Development. Set it via configuration or user-secrets.")]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Short-lived by design (OWASP): a stolen access token expires fast; refresh handles longevity.</summary>
    [Range(1, int.MaxValue)]
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token absolute lifetime, 1 to 36,500 days. Rotated on every use; reuse triggers family revocation.
    /// </summary>
    // The ceiling (a century) keeps "now + lifetime" far inside DateTimeOffset's range; int.MaxValue days would overflow.
    [Range(1, 36_500)]
    public int RefreshTokenDays { get; set; } = 7;
}

/// <summary>Source-generated validator for the data-annotation rules on <see cref="JwtOptions"/> (R-048).</summary>
[OptionsValidator]
internal sealed partial class JwtOptionsValidator : IValidateOptions<JwtOptions>;
