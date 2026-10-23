using System.Buffers.Text;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using NovaCore.AspNetCore.Auth;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// The links the provider emails (email confirmation, password reset): an absolute URL under <c>Jwt:Authority</c>, never
/// built from the request's Host header (host header poisoning), carrying the user id and an Identity token. The token
/// travels in Base64Url, since Identity's tokens contain characters a URL would mangle.
/// </summary>
internal static class EmailLinks
{
    /// <summary>The link to <paramref name="path"/> (relative to the authority) with the user id and the token.</summary>
    public static string Create(JwtOptions jwt, string path, Guid userId, string token) =>
        QueryHelpers.AddQueryString(
            new Uri(new Uri(jwt.Authority.TrimEnd('/') + "/"), path.TrimStart('/')).AbsoluteUri,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["userId"] = userId.ToString(),
                ["token"] = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(token))
            });

    /// <summary>The Identity token of a link, or <see langword="null"/> when it isn't Base64Url.</summary>
    public static string? DecodeToken(string token)
    {
        try
        {
            return Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
