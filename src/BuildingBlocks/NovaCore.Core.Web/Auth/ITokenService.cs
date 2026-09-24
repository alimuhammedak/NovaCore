using System.Security.Claims;

namespace NovaCore.Core.Web.Auth;

/// <summary>Signs short-lived access tokens. The single seam for access-token creation across the app.</summary>
public interface ITokenService
{
    /// <summary>Signs an access token carrying exactly the supplied claims (plus jti/iat/nbf/exp).</summary>
    AccessToken CreateAccessToken(IReadOnlyCollection<Claim> claims);
}

/// <summary>A freshly minted access token and when it expires.</summary>
public readonly record struct AccessToken(string Token, DateTimeOffset ExpiresAtUtc)
{
    public int ExpiresInSeconds => (int)Math.Max(0, (ExpiresAtUtc - DateTimeOffset.UtcNow).TotalSeconds);
}
