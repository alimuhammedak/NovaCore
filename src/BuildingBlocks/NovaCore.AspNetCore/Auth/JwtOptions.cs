using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace NovaCore.AspNetCore.Auth;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section and validated when the host starts. Access tokens are validated
/// against <see cref="Authority"/>, an OpenID Connect provider; <see cref="SigningKey"/> is a Development-only
/// alternative, which a local token issuer also signs with.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// The OpenID Connect provider that issues access tokens, e.g. <c>https://idp.example.com/realms/novacore</c>. Its
    /// issuer and signing keys are discovered from <c>{Authority}/.well-known/openid-configuration</c> and its JWKS.
    /// Required outside Development, where it must be an absolute <c>https</c> URI.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// The <c>iss</c> claim of tokens signed with <see cref="SigningKey"/>. Tokens from <see cref="Authority"/> are held
    /// to the issuer in its discovery document instead.
    /// </summary>
    [Required]
    public string Issuer { get; set; } = "novacore";

    /// <summary>The <c>aud</c> claim every access token must carry (and a local issuer writes).</summary>
    [Required]
    public string Audience { get; set; } = "novacore.api";

    /// <summary>
    /// A symmetric HS256 key, at least 32 bytes (256 bits), accepted in Development only: anywhere else the host refuses
    /// to start with one. In Development, when neither this nor <see cref="Authority"/> is configured, a throwaway key
    /// is used so the app runs out of the box.
    /// </summary>
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
