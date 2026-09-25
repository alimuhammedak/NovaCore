using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace NovaCore.Core.Web.Auth;

/// <summary>Creates HS256-signed JWT access tokens using the configured <see cref="JwtOptions"/>.</summary>
public sealed class JwtTokenService(JwtOptions options) : ITokenService
{
    private static readonly JsonWebTokenHandler Handler = new();

    /// <inheritdoc />
    public AccessToken CreateAccessToken(IReadOnlyCollection<Claim> claims)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(options.AccessTokenMinutes);

        var identity = new ClaimsIdentity(claims);

        // Always stamp a unique token id (jti) for traceability / potential denylisting.
        if (!identity.HasClaim(c => c.Type == JwtRegisteredClaimNames.Jti))
            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = identity,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        });

        return new AccessToken(token, expires);
    }
}
