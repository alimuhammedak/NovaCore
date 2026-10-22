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

    // Only after the right password (CheckUnconfirmedAsync), so it reveals nothing to someone who doesn't know it.
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

    // The forms are read and their antiforgery token checked here, not by [FromForm] binding (see HtmlPage).
    private static async Task<IResult> SignIn(
        HttpContext context,
        IAntiforgery antiforgery,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        IPasswordHasher<ApplicationUser> hasher,
        DummyPasswordHash dummyHash,
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
            // An unknown address costs a password hash too, so the answer doesn't come sooner than for a known one.
            if (user is null && !string.IsNullOrEmpty(password))
                dummyHash.Check(hasher, password);

            audit.LoginFailed(null, ip, "unknown-user");
            return PasswordForm(context, antiforgery, returnUrl, email, InvalidCredentials);
        }

        var result = await signIn.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
        if (result.IsNotAllowed)
            result = await CheckUnconfirmedAsync(users, user, password);

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
            audit.LoginFailed(user.Id, ip, result.IsNotAllowed ? "email-not-confirmed" : "bad-password");

        return PasswordForm(context, antiforgery, returnUrl, email, result.IsNotAllowed ? EmailNotConfirmed : InvalidCredentials);
    }

    // SignInManager answers NotAllowed (email not confirmed) before it looks at the password, so the password is checked
    // here: the "confirm your email" hint goes only to someone who knows it, and a wrong one counts toward lockout as
    // anywhere else (docs/plan/RESEARCH.md R-067).
    private static async Task<SignInResult> CheckUnconfirmedAsync(UserManager<ApplicationUser> users, ApplicationUser user, string password)
    {
        if (await users.IsLockedOutAsync(user))
            return SignInResult.LockedOut;

        if (await users.CheckPasswordAsync(user, password))
            return SignInResult.NotAllowed;

        await users.AccessFailedAsync(user);
        return await users.IsLockedOutAsync(user) ? SignInResult.LockedOut : SignInResult.Failed;
    }

    private static async Task<IResult> ShowTwoFactor(
        string? returnUrl, HttpContext context, IAntiforgery antiforgery, SignInManager<ApplicationUser> signIn) =>
        await signIn.GetTwoFactorAuthenticationUserAsync() is null
            ? Results.LocalRedirect(QueryHelpers.AddQueryString(context.Request.PathBase + Path, "ReturnUrl", returnUrl ?? string.Empty))
            : CodeForm(context, antiforgery, returnUrl, error: null);

    private static async Task<IResult> SignInWithCode(
        HttpContext context,
        IAntiforgery antiforgery,
        UserManager<ApplicationUser> users,
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

        // An authenticator code is digits, even mistyped; Identity's recovery codes (XXXXX-XXXXX) always have a dash.
        var entered = (code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal);
        var isRecoveryCode = entered.Length == 0 || !entered.All(char.IsAsciiDigit);

        // A wrong code of either kind counts as a failed access, so it leads to lockout like a wrong password.
        var result = isRecoveryCode
            ? await RecoveryCodeSignInAsync(users, signIn, user, entered)
            : await signIn.TwoFactorAuthenticatorSignInAsync(entered, isPersistent: false, rememberClient: false);

        if (result.Succeeded)
        {
            if (isRecoveryCode)
                audit.RecoveryCodeUsed(user.Id);
            audit.LoginSucceeded(user.Id, ip);
            return Continue(returnUrl);
        }

        if (result.IsLockedOut)
            audit.AccountLockedOut(user.Id, ip);
        else
            audit.LoginFailed(user.Id, ip, isRecoveryCode ? "bad-recovery-code" : "bad-2fa-code");

        return CodeForm(context, antiforgery, returnUrl, isRecoveryCode ? InvalidRecoveryCode : InvalidCode);
    }

    // SignInManager redeems a recovery code without looking at lockout and doesn't count a wrong one: "codes are expected
    // to be random" (docs/plan/RESEARCH.md R-085). Both happen here, so a locked account can't get in with a code and a
    // wrong code counts like a wrong authenticator code. A redeemed code is gone (single use).
    private static async Task<SignInResult> RecoveryCodeSignInAsync(
        UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, ApplicationUser user, string code)
    {
        if (await users.IsLockedOutAsync(user))
            return SignInResult.LockedOut;

        // The codes are upper case letters and digits.
        var result = await signIn.TwoFactorRecoveryCodeSignInAsync(code.ToUpperInvariant());
        if (result.Succeeded)
            return result;

        await users.AccessFailedAsync(user);
        return await users.IsLockedOutAsync(user) ? SignInResult.LockedOut : SignInResult.Failed;
    }

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
