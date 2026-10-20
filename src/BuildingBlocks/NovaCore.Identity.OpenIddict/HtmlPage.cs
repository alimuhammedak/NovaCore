using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// The provider's browser pages (sign-in, password reset, sign-out): plain HTML without scripts or styles, which the
/// core's Content-Security-Policy forbids, and forms that post back to this application with an antiforgery token. The
/// handlers check the token themselves with <c>IAntiforgery.IsRequestValidAsync</c>: the host pipeline has no antiforgery
/// middleware (docs/plan/RESEARCH.md R-061).
/// </summary>
internal static class HtmlPage
{
    /// <summary>
    /// A page with a form posting to <paramref name="action"/> in this application. <paramref name="fields"/> and
    /// <paramref name="after"/> are markup the caller encoded; the title, the error and the hidden fields are encoded here.
    /// </summary>
    public static IResult Form(
        HttpContext context,
        IAntiforgery antiforgery,
        string title,
        string action,
        IEnumerable<KeyValuePair<string, string?>> hidden,
        string? error,
        string fields,
        string submit = "Continue",
        string after = "")
    {
        var html = HtmlEncoder.Default;
        var tokens = antiforgery.GetAndStoreTokens(context);
        var inputs = string.Concat(hidden
            .Prepend(new(tokens.FormFieldName, tokens.RequestToken))
            .Select(field => $"""
                  <input type="hidden" name="{html.Encode(field.Key)}" value="{html.Encode(field.Value ?? "")}">

                """));

        return Render(title, $"""
            <h1>{html.Encode(title)}</h1>
            {(error is null ? "" : $"<p role=\"alert\">{html.Encode(error)}</p>")}
            <form method="post" action="{html.Encode(context.Request.PathBase + action)}">
            {inputs}{fields}  <p><button type="submit">{html.Encode(submit)}</button></p>
            </form>
            {after}
            """);
    }

    /// <summary>A page with a heading and one paragraph of text.</summary>
    public static IResult Message(string title, string text) =>
        Render(title, $"<h1>{HtmlEncoder.Default.Encode(title)}</h1><p>{HtmlEncoder.Default.Encode(text)}</p>");

    /// <summary>A paragraph with a link to <paramref name="path"/> in this application.</summary>
    public static string Link(HttpContext context, string path, string text) =>
        $"<p><a href=\"{HtmlEncoder.Default.Encode(context.Request.PathBase + path)}\">{HtmlEncoder.Default.Encode(text)}</a></p>";

    /// <summary>A whole page around <paramref name="body"/>, which is markup the caller encoded.</summary>
    public static IResult Render(string title, string body) => Results.Content(
        $"""
         <!doctype html>
         <html lang="en">
         <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{HtmlEncoder.Default.Encode(title)}</title></head>
         <body><main>
         {body}
         </main></body>
         </html>
         """,
        "text/html; charset=utf-8");
}
