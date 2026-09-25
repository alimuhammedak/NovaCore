using System.Security.Claims;

namespace NovaCore.AspNetCore.Auth;

/// <summary>Signs short-lived access tokens. The single seam for access-token creation across the app.</summary>
public interface ITokenService
{
    /// <summary>Signs an access token carrying exactly the supplied claims (plus jti/iat/nbf/exp).</summary>
    AccessToken CreateAccessToken(IReadOnlyCollection<Claim> claims);
}

/// <summary>A freshly minted access token, when it was issued and when it expires.</summary>
public readonly record struct AccessToken(string Token, DateTimeOffset IssuedAtUtc, DateTimeOffset ExpiresAtUtc)
{
    /// <summary>
    /// The token's lifetime in seconds, from <see cref="IssuedAtUtc"/> to <see cref="ExpiresAtUtc"/> (OAuth
    /// <c>expires_in</c>); never negative.
    /// </summary>
    public int ExpiresInSeconds => (int)Math.Max(0, (ExpiresAtUtc - IssuedAtUtc).TotalSeconds);
}
