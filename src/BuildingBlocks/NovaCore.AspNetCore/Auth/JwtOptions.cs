using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace NovaCore.AspNetCore.Auth;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section and validated when the host starts. Access tokens are validated
/// against <see cref="Authority"/>, an OpenID Connect provider, and must be issued for <see cref="Audience"/>.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// The OpenID Connect provider that issues access tokens, e.g. <c>https://idp.example.com/realms/novacore</c>. Its
    /// issuer and signing keys are discovered from <c>{Authority}/.well-known/openid-configuration</c> and its JWKS.
    /// Required in every environment; an absolute <c>https</c> URI outside Development.
    /// </summary>
    [Required]
    public string Authority { get; set; } = string.Empty;

    /// <summary>The <c>aud</c> claim every access token must carry.</summary>
    [Required]
    public string Audience { get; set; } = "novacore.api";
}

/// <summary>Source-generated validator for the data-annotation rules on <see cref="JwtOptions"/> (R-048).</summary>
[OptionsValidator]
internal sealed partial class JwtOptionsValidator : IValidateOptions<JwtOptions>;
