using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// The sign-in pages the authorization endpoint sends an anonymous browser to: the password step, then the
/// authenticator-code step for an account with two-factor authentication. Plain HTML without scripts or styles (the
/// core's Content-Security-Policy allows neither), an antiforgery token, and a redirect back to a local URL only.
/// </summary>
internal static class LoginPage
{
    public const string Path = "/account/login";
    public const string TwoFactorPath = "/account/login/2fa";

    // One message for an unknown account, a wrong password and a locked account, so the page doesn't reveal which.
    private const string InvalidCredentials = "Invalid email or password.";

    // Only after the right password, so it reveals nothing to someone who doesn't know it.
    private const string EmailNotConfirmed = "Confirm your email address before signing in.";

    private const string InvalidCode = "Invalid authenticator code.";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var account = endpoints.MapGroup("/account").ExcludeFromDescription().RequireRateLimiting("strict");

        account.MapGet("/login", (string? returnUrl, HttpContext context, IAntiforgery antiforgery) =>
            PasswordForm(context, antiforgery, returnUrl, email: null, error: null));
        account.MapPost("/login", SignIn);

        account.MapGet("/login/2fa", ShowTwoFactor);
        account.MapPost("/login/2fa", SignInWithCode);
    }

    // The forms are read and their antiforgery token checked here, not by [FromForm] binding: that needs the antiforgery
    // middleware, which the host pipeline doesn't have (docs/plan/RESEARCH.md R-061).
    private static async Task<IResult> SignIn(
        HttpContext context,
        IAntiforgery antiforgery,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        SecurityAuditLogger audit)
    {
        if (!context.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        string? email = form["Email"], password = form["Password"], returnUrl = form["ReturnUrl"];
        var ip = context.Connection.RemoteIpAddress?.ToString();

        var user = string.IsNullOrWhiteSpace(email) ? null : await users.FindByEmailAsync(email);
        if (user is null || string.IsNullOrEmpty(password))
        {
            audit.LoginFailed(email, ip, "unknown-user");
            return PasswordForm(context, antiforgery, returnUrl, email, InvalidCredentials);
        }

        var result = await signIn.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            audit.LoginSucceeded(user.Id, ip);
            return Continue(returnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            // SignInManager remembers the user in the two-factor cookie until the code step.
            audit.TwoFactorChallenged(user.Id);
            return Results.LocalRedirect(QueryHelpers.AddQueryString(
                context.Request.PathBase + TwoFactorPath, "ReturnUrl", returnUrl ?? string.Empty));
        }

        if (result.IsLockedOut)
            audit.AccountLockedOut(user.Id, ip);
        else
            audit.LoginFailed(email, ip, result.IsNotAllowed ? "email-not-confirmed" : "bad-password");

        return PasswordForm(context, antiforgery, returnUrl, email, result.IsNotAllowed ? EmailNotConfirmed : InvalidCredentials);
    }

    private static async Task<IResult> ShowTwoFactor(
        string? returnUrl, HttpContext context, IAntiforgery antiforgery, SignInManager<ApplicationUser> signIn) =>
        await signIn.GetTwoFactorAuthenticationUserAsync() is null
            ? Results.LocalRedirect(QueryHelpers.AddQueryString(context.Request.PathBase + Path, "ReturnUrl", returnUrl ?? string.Empty))
            : CodeForm(context, antiforgery, returnUrl, error: null);

    private static async Task<IResult> SignInWithCode(
        HttpContext context,
        IAntiforgery antiforgery,
        SignInManager<ApplicationUser> signIn,
        SecurityAuditLogger audit)
    {
        if (!context.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        string? code = form["Code"], returnUrl = form["ReturnUrl"];
        var ip = context.Connection.RemoteIpAddress?.ToString();

        // Only after the password step: the two-factor cookie names the user.
        var user = await signIn.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
            return Results.LocalRedirect(QueryHelpers.AddQueryString(context.Request.PathBase + Path, "ReturnUrl", returnUrl ?? string.Empty));

        // A wrong code counts as a failed access, so it leads to lockout like a wrong password.
        var result = await signIn.TwoFactorAuthenticatorSignInAsync(
            (code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal), isPersistent: false, rememberClient: false);

        if (result.Succeeded)
        {
            audit.LoginSucceeded(user.Id, ip);
            return Continue(returnUrl);
        }

        if (result.IsLockedOut)
            audit.AccountLockedOut(user.Id, ip);
        else
            audit.LoginFailed(user.Email, ip, "bad-2fa-code");

        return CodeForm(context, antiforgery, returnUrl, InvalidCode);
    }

    // Only back into this application (the authorization endpoint): never an open redirect.
    private static IResult Continue(string? returnUrl) =>
        RedirectHttpResult.IsLocalUrl(returnUrl)
            ? Results.LocalRedirect(returnUrl!)
            : Page("Signed in", "<h1>Signed in</h1><p>You are signed in. Return to the application to continue.</p>");

    private static IResult PasswordForm(HttpContext context, IAntiforgery antiforgery, string? returnUrl, string? email, string? error) =>
        Form(context, antiforgery, "Sign in", Path, returnUrl, error, $"""
            <p><label for="email">Email</label><br><input id="email" name="Email" type="email" autocomplete="username" required value="{HtmlEncoder.Default.Encode(email ?? "")}"></p>
            <p><label for="password">Password</label><br><input id="password" name="Password" type="password" autocomplete="current-password" required></p>
            """);

    private static IResult CodeForm(HttpContext context, IAntiforgery antiforgery, string? returnUrl, string? error) =>
        Form(context, antiforgery, "Two-factor authentication", TwoFactorPath, returnUrl, error, """
            <p><label for="code">Authenticator code</label><br><input id="code" name="Code" inputmode="numeric" autocomplete="one-time-code" required></p>
            """);

    private static IResult Form(
        HttpContext context, IAntiforgery antiforgery, string title, string action, string? returnUrl, string? error, string fields)
    {
        var html = HtmlEncoder.Default;
        var tokens = antiforgery.GetAndStoreTokens(context);

        return Page(title, $"""
            <h1>{html.Encode(title)}</h1>
            {(error is null ? "" : $"<p role=\"alert\">{html.Encode(error)}</p>")}
            <form method="post" action="{html.Encode(context.Request.PathBase + action)}">
              <input type="hidden" name="{html.Encode(tokens.FormFieldName)}" value="{html.Encode(tokens.RequestToken ?? "")}">
              <input type="hidden" name="ReturnUrl" value="{html.Encode(returnUrl ?? "")}">
            {fields}  <p><button type="submit">Continue</button></p>
            </form>
            """);
    }

    private static IResult Page(string title, string body) => Results.Content(
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
