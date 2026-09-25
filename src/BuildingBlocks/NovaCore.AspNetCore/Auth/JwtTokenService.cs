using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace NovaCore.AspNetCore.Auth;

/// <summary>
/// Creates HS256-signed JWT access tokens using the configured <see cref="JwtOptions"/>, stamped with the time from the
/// injected <see cref="TimeProvider"/>.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private static readonly JsonWebTokenHandler Handler = new();

    /// <inheritdoc />
    public AccessToken CreateAccessToken(IReadOnlyCollection<Claim> claims)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(jwt.AccessTokenMinutes);

        var identity = new ClaimsIdentity(claims);

        // Always stamp a unique token id (jti) for traceability / potential denylisting.
        if (!identity.HasClaim(c => c.Type == JwtRegisteredClaimNames.Jti))
            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

        var token = Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Subject = identity,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        });

        return new AccessToken(token, now, expires);
    }
}
