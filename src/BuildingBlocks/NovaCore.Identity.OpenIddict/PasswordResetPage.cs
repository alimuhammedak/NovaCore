using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Auth;
using OpenIddict.Abstractions;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Password reset, reached from the sign-in page: a form that emails a reset link to a confirmed account, and the page the
/// link opens, which sets a new password. Neither answer reveals whether an address has an account. A reset ends every
/// session of the account, and the user then signs in as usual (OWASP Forgot Password Cheat Sheet; the checks follow
/// Identity's own <c>/forgotPassword</c> and <c>/resetPassword</c>, docs/plan/RESEARCH.md R-084).
/// </summary>
internal static class PasswordResetPage
{
    public const string ForgotPath = "/account/forgot-password";
    public const string ResetPath = "/account/reset-password";

    private const string LinkSent = "If an account uses this address, a link to reset its password has been sent to it.";

    // One answer for an unknown account, an unconfirmed one, a malformed token and an expired or used one.
    private const string InvalidLink = "This link is invalid or has expired. Ask for a new one from the sign-in page.";

    private const string Mismatch = "The passwords do not match.";

    // The account API's limit (AccountRequests): the password policy sets the minimum.
    private const int MaxPasswordLength = 128;

    public static void Map(RouteGroupBuilder account)
    {
        account.MapGet("/forgot-password", (HttpContext context, IAntiforgery antiforgery) => EmailForm(context, antiforgery, error: null));
        account.MapPost("/forgot-password", RequestLink);

        account.MapGet("/reset-password", (string? userId, string? token, HttpContext context, IAntiforgery antiforgery) =>
            string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token)
                ? HtmlPage.Message("Reset your password", InvalidLink)
                : PasswordForm(context, antiforgery, userId, token, error: null));
        account.MapPost("/reset-password", Reset);
    }

    private static async Task<IResult> RequestLink(
        HttpContext context,
        IAntiforgery antiforgery,
        UserManager<ApplicationUser> users,
        EmailQueue emails,
        IOptions<JwtOptions> jwt,
        SecurityAuditLogger audit)
    {
        if (!context.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        string? email = form["Email"];
        if (string.IsNullOrWhiteSpace(email))
            return EmailForm(context, antiforgery, "Enter your email address.");

        // Only a confirmed account gets a link; the answer is the same either way.
        var user = await users.FindByEmailAsync(email);
        if (user is not null && await users.IsEmailConfirmedAsync(user))
        {
            var link = EmailLinks.Create(jwt.Value, ResetPath, user.Id, await users.GeneratePasswordResetTokenAsync(user));
            // Queued, not sent here: the answer mustn't wait for the mail server, or its timing would reveal the account.
            emails.Enqueue(user.Email!, "Reset your password",
                $"Set a new password: <a href=\"{HtmlEncoder.Default.Encode(link)}\">Reset your password</a>. "
                + "If you didn't ask for this, ignore this email; your password stays the same.");
            audit.PasswordResetRequested(user.Id);
        }

        return HtmlPage.Message("Check your email", LinkSent);
    }

    private static async Task<IResult> Reset(
        HttpContext context,
        IAntiforgery antiforgery,
        UserManager<ApplicationUser> users,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        SecurityAuditLogger audit)
    {
        if (!context.Request.HasFormContentType || !await antiforgery.IsRequestValidAsync(context))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        string? userId = form["UserId"], token = form["Token"], password = form["Password"], confirmation = form["ConfirmPassword"];

        if (!Guid.TryParse(userId, out var id) || string.IsNullOrEmpty(token))
            return HtmlPage.Message("Reset your password", InvalidLink);

        if (string.IsNullOrEmpty(password) || !string.Equals(password, confirmation, StringComparison.Ordinal))
            return PasswordForm(context, antiforgery, userId, token, Mismatch);

        if (password.Length > MaxPasswordLength)
            return PasswordForm(context, antiforgery, userId, token, $"Passwords must be at most {MaxPasswordLength} characters.");

        // An unknown or unconfirmed account answers like a bad token.
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null || !await users.IsEmailConfirmedAsync(user) || EmailLinks.DecodeToken(token) is not { } decoded)
            return HtmlPage.Message("Reset your password", InvalidLink);

        // The token is checked before the password policy, so a policy error means the link is still good.
        var result = await users.ResetPasswordAsync(user, decoded, password);
        if (!result.Succeeded)
        {
            return result.Errors.Any(error => string.Equals(error.Code, nameof(IdentityErrorDescriber.InvalidToken), StringComparison.Ordinal))
                ? HtmlPage.Message("Reset your password", InvalidLink)
                : PasswordForm(context, antiforgery, userId, token, string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        // ResetPasswordAsync renewed the security stamp: the link can't be used again and the provider's sign-in cookie
        // ends. The refresh tokens go too, since a reset may follow a compromise.
        await AccountEndpoints.RevokeSessionsAsync(user, tokens, authorizations, context.RequestAborted);
        audit.PasswordReset(user.Id);
        audit.SessionsRevoked(user.Id, "password-reset");

        return HtmlPage.Message("Password reset", "Your password has been reset. Return to the application and sign in with the new password.");
    }

    private static IResult EmailForm(HttpContext context, IAntiforgery antiforgery, string? error) =>
        HtmlPage.Form(context, antiforgery, "Forgot your password?", ForgotPath, [], error, """
            <p>Enter the email address of your account. If it has one, we send it a link to set a new password.</p>
            <p><label for="email">Email</label><br><input id="email" name="Email" type="email" autocomplete="username" required></p>
            """,
            submit: "Send the link");

    private static IResult PasswordForm(HttpContext context, IAntiforgery antiforgery, string userId, string token, string? error) =>
        HtmlPage.Form(context, antiforgery, "Reset your password", ResetPath, [new("UserId", userId), new("Token", token)], error, """
            <p><label for="password">New password</label><br><input id="password" name="Password" type="password" autocomplete="new-password" required></p>
            <p><label for="confirm">New password again</label><br><input id="confirm" name="ConfirmPassword" type="password" autocomplete="new-password" required></p>
            """,
            submit: "Set the password");
}
