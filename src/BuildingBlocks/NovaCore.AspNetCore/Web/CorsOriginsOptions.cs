namespace NovaCore.AspNetCore.Web;

/// <summary>
/// Bound from the <c>Cors</c> section. Empty allows any origin without credentials; otherwise only these origins, with
/// credentials. Named apart from ASP.NET Core's own <c>CorsOptions</c>, which it configures.
/// </summary>
internal sealed class CorsOriginsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>
    /// An origin is exactly scheme + host (+ port): the browser's <c>Origin</c> header never carries a path or a trailing
    /// slash, so such an entry would never match, and <c>*</c> together with credentials fails on the first CORS request.
    /// </summary>
    public static bool IsOrigin(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && string.Equals(uri.GetLeftPart(UriPartial.Authority), value, StringComparison.OrdinalIgnoreCase);
}
