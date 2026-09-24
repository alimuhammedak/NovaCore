namespace NovaCore.Modules.Identity.Services;

/// <summary>The token pair returned to a client after login or refresh.</summary>
public sealed record AuthTokens(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
