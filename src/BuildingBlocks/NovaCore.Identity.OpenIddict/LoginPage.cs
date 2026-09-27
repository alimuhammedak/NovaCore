using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// The sign-in page the authorization endpoint sends an anonymous browser to: plain HTML without scripts or styles
/// (the core's Content-Security-Policy allows neither), an antiforgery token, and a redirect back to a local URL only.
/// </summary>
internal static class LoginPage
{
    public const string Path = "/account/login";

    private const string InvalidCredentials = "Invalid email or password.";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var account = endpoints.MapGroup("/account").ExcludeFromDescription().RequireRateLimiting("strict");

        account.MapGet("/login", (string? returnUrl, HttpContext context, IAntiforgery antiforgery) =>
            Render(context, antiforgery, returnUrl, email: null, error: null));

        account.MapPost("/login", SignIn);
    }

    // The form is read and its antiforgery token checked here, not by [FromForm] binding: that needs the antiforgery
    // middleware, which the host pipeline doesn't have (docs/plan/RESEARCH.md R-061).
    private static async Task<IResult> SignIn(
        HttpContext context,
        IAntiforgery antiforgery,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn)
    {
        if (!context.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        string? email = form["Email"], password = form["Password"], returnUrl = form["ReturnUrl"];

        var user = string.IsNullOrWhiteSpace(email) ? null : await users.FindByEmailAsync(email);
        if (user is null || string.IsNullOrEmpty(password))
            return Render(context, antiforgery, returnUrl, email, InvalidCredentials);

        var result = await signIn.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Render(context, antiforgery, returnUrl, email, InvalidCredentials);

        return Continue(returnUrl);
    }

    // Only back into this application (the authorization endpoint): never an open redirect.
    private static IResult Continue(string? returnUrl) =>
        RedirectHttpResult.IsLocalUrl(returnUrl)
            ? Results.LocalRedirect(returnUrl!)
            : Page("Signed in", "<h1>Signed in</h1><p>You are signed in. Return to the application to continue.</p>");

    private static IResult Render(HttpContext context, IAntiforgery antiforgery, string? returnUrl, string? email, string? error)
    {
        var html = HtmlEncoder.Default;
        var tokens = antiforgery.GetAndStoreTokens(context);

        var body = $"""
            <h1>Sign in</h1>
            {(error is null ? "" : $"<p role=\"alert\">{html.Encode(error)}</p>")}
            <form method="post" action="{html.Encode(context.Request.PathBase + Path)}">
              <input type="hidden" name="{html.Encode(tokens.FormFieldName)}" value="{html.Encode(tokens.RequestToken ?? "")}">
              <input type="hidden" name="ReturnUrl" value="{html.Encode(returnUrl ?? "")}">
              <p><label for="email">Email</label><br><input id="email" name="Email" type="email" autocomplete="username" required value="{html.Encode(email ?? "")}"></p>
              <p><label for="password">Password</label><br><input id="password" name="Password" type="password" autocomplete="current-password" required></p>
              <p><button type="submit">Sign in</button></p>
            </form>
            """;

        return Page("Sign in", body);
    }

    private static IResult Page(string title, string body) => Results.Content(
        $"""
         <!doctype html>
         <html lang="en">
         <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{title}</title></head>
         <body><main>
         {body}
         </main></body>
         </html>
         """,
        "text/html; charset=utf-8");
}
