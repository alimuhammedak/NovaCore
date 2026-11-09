using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using NovaCore.Abstractions.Results;
using NovaCore.AspNetCore.Http;
using OpenIddict.Abstractions;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Password reset with a code for a project's own screen (docs/plan/DECISIONS.md D-091 (4)): ask for a code, then send it
/// with the new password. The rules are <see cref="PasswordResetCodes"/>'; the answers reveal no account, like the reset
/// form's (D-038 (2)). A reset ends every session and signs nobody in (D-038 (3)).
/// </summary>
internal static class PasswordCodeEndpoints
{
    private static readonly AccountEndpoints.MessageResponse CodeSent =
        new("If a confirmed account uses this address, a code to reset its password has been sent to it.");

    private static readonly AccountEndpoints.MessageResponse PasswordReset =
        new("Your password has been reset. Sign in with the new password.");

    // One answer for an unknown account, an unconfirmed one and a wrong, expired, used or blocked code.
    private static readonly Error InvalidResetCode =
        Error.Validation("account.invalid_reset_code", "The code is invalid or has expired. Ask for a new one.");

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var password = endpoints.MapGroup("/api/v1/auth/password").WithTags("Auth").RequireRateLimiting("strict")
            .AllowAnonymous().WithMetadata(new DisableCorsAttribute());

        password.MapPost("/forgot", Forgot)
            .WithName("PasswordForgot")
            .WithSummary("Email a code to reset the password (only to a confirmed account; the same answer for every address)")
            .ProducesValidationProblem();

        password.MapPost("/reset", Reset)
            .WithName("PasswordReset")
            .WithSummary("Set a new password with the emailed code (ends every session, signs nobody in)")
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<Accepted<AccountEndpoints.MessageResponse>> Forgot(
        PasswordForgotRequest request,
        UserManager<ApplicationUser> users,
        PasswordResetCodes codes,
        EmailQueue emails,
        SecurityAuditLogger audit)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is not null && await users.IsEmailConfirmedAsync(user))
        {
            var code = await codes.IssueAsync(user);
            // Queued, not sent here: the answer mustn't wait for the mail server, or its timing would reveal the account.
            emails.Enqueue(user.Email!, "Your password reset code",
                $"Your code to reset your password: <strong>{code}</strong>. It works once, for "
                + $"{PasswordResetCodes.Lifetime.TotalMinutes:0} minutes. If you didn't ask for it, ignore this email; your "
                + "password stays the same.");
            audit.PasswordResetRequested(user.Id);
        }

        return TypedResults.Accepted((string?)null, CodeSent);
    }

    private static async Task<Results<Ok<AccountEndpoints.MessageResponse>, ValidationProblem, ProblemHttpResult>> Reset(
        PasswordResetCodeRequest request,
        UserManager<ApplicationUser> users,
        PasswordResetCodes codes,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        SecurityAuditLogger audit,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !await users.IsEmailConfirmedAsync(user))
            return ResultHttpExtensions.Problem(InvalidResetCode);

        var (outcome, errors) = await codes.RedeemAsync(user, request.Code, request.NewPassword);
        switch (outcome)
        {
            case ResetCodeOutcome.PasswordRejected:
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal) { ["newPassword"] = [.. errors] });
            case ResetCodeOutcome.InvalidCode:
                audit.PasswordResetCodeRejected(user.Id);
                return ResultHttpExtensions.Problem(InvalidResetCode);
        }

        // The reset renewed the security stamp (the provider's sign-in cookie ends); the refresh tokens go too, since a
        // reset may follow a compromise.
        await AccountEndpoints.RevokeSessionsAsync(user, tokens, authorizations, cancellationToken);
        audit.PasswordReset(user.Id);
        audit.SessionsRevoked(user.Id, "password-reset");
        return TypedResults.Ok(PasswordReset);
    }
}
