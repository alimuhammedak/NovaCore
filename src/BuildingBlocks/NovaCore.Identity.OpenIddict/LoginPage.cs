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
/// The sign-in pages the authorization endpoint sends an anonymous browser to: the password step, then, for an account
/// with two-factor authentication, the code step, which takes an authenticator code or one of the recovery codes. Plain
/// HTML without scripts or styles (the core's Content-Security-Policy allows neither), an antiforgery token, and a
/// redirect back to a local URL only.
/// </summary>
internal static class LoginPage
{
    public const string Path = "/account/login";
    public const string TwoFactorPath = "/account/login/2fa";

    // One message for an unknown account, a wrong password and a locked account, so the page doesn't reveal which.
    private const string InvalidCredentials = "Invalid email or password.";

    // Only after the right password (SignInFlow.CheckUnconfirmedAsync), so it reveals nothing to someone who doesn't know it.
    private const string EmailNotConfirmed = "Confirm your email address before signing in.";

    private const string InvalidCode = "Invalid authenticator code.";

    private const string InvalidRecoveryCode = "Invalid recovery code.";

    public static void Map(RouteGroupBuilder account)
    {
        account.MapGet("/login", (string? returnUrl, HttpContext context, IAntiforgery antiforgery) =>
            PasswordForm(context, antiforgery, returnUrl, email: null, error: null));
        account.MapPost("/login", SignIn);

        account.MapGet("/login/2fa", ShowTwoFactor);
        account.MapPost("/login/2fa", SignInWithCode);
    }

    // The forms are read and their antiforgery token checked here, not by [FromForm] binding (see HtmlPage). The rules
    // themselves are SignInFlow's, shared with the JSON endpoints.
    private static async Task<IResult> SignIn(HttpContext context, IAntiforgery antiforgery, SignInFlow flow)
    {
        if (!context.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        string? email = form["Email"], password = form["Password"], returnUrl = form["ReturnUrl"];

        return await flow.PasswordAsync(email, password, context.Connection.RemoteIpAddress?.ToString()) switch
        {
            SignInOutcome.Succeeded => Continue(returnUrl),
            SignInOutcome.TwoFactorRequired => Results.LocalRedirect(QueryHelpers.AddQueryString(
                context.Request.PathBase + TwoFactorPath, "ReturnUrl", returnUrl ?? string.Empty)),
            SignInOutcome.EmailNotConfirmed => PasswordForm(context, antiforgery, returnUrl, email, EmailNotConfirmed),
            _ => PasswordForm(context, antiforgery, returnUrl, email, InvalidCredentials),
        };
    }

    private static async Task<IResult> ShowTwoFactor(
        string? returnUrl, HttpContext context, IAntiforgery antiforgery, SignInManager<ApplicationUser> signIn) =>
        await signIn.GetTwoFactorAuthenticationUserAsync() is null
            ? BackToPassword(context, returnUrl)
            : CodeForm(context, antiforgery, returnUrl, error: null);

    private static async Task<IResult> SignInWithCode(HttpContext context, IAntiforgery antiforgery, SignInFlow flow)
    {
        if (!context.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        string? code = form["Code"], returnUrl = form["ReturnUrl"];

        return await flow.CodeAsync(code, context.Connection.RemoteIpAddress?.ToString()) switch
        {
            SignInOutcome.Succeeded => Continue(returnUrl),
            SignInOutcome.NoTwoFactorSession => BackToPassword(context, returnUrl),
            SignInOutcome.InvalidRecoveryCode => CodeForm(context, antiforgery, returnUrl, InvalidRecoveryCode),
            _ => CodeForm(context, antiforgery, returnUrl, InvalidCode),
        };
    }

    private static IResult BackToPassword(HttpContext context, string? returnUrl) =>
        Results.LocalRedirect(QueryHelpers.AddQueryString(context.Request.PathBase + Path, "ReturnUrl", returnUrl ?? string.Empty));

    // Only back into this application (the authorization endpoint): never an open redirect.
    private static IResult Continue(string? returnUrl) =>
        RedirectHttpResult.IsLocalUrl(returnUrl)
            ? Results.LocalRedirect(returnUrl!)
            : HtmlPage.Message("Signed in", "You are signed in. Return to the application to continue.");

    private static IResult PasswordForm(HttpContext context, IAntiforgery antiforgery, string? returnUrl, string? email, string? error) =>
        HtmlPage.Form(context, antiforgery, "Sign in", Path, [new("ReturnUrl", returnUrl)], error, $"""
            <p><label for="email">Email</label><br><input id="email" name="Email" type="email" autocomplete="username" required value="{HtmlEncoder.Default.Encode(email ?? "")}"></p>
            <p><label for="password">Password</label><br><input id="password" name="Password" type="password" autocomplete="current-password" required></p>
            """,
            after: HtmlPage.Link(context, PasswordResetPage.ForgotPath, "Forgot your password?"));

    private static IResult CodeForm(HttpContext context, IAntiforgery antiforgery, string? returnUrl, string? error) =>
        HtmlPage.Form(context, antiforgery, "Two-factor authentication", TwoFactorPath, [new("ReturnUrl", returnUrl)], error, """
            <p><label for="code">Authenticator code or recovery code</label><br><input id="code" name="Code" autocomplete="one-time-code" required></p>
            """);
}
