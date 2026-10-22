using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NovaCore.Abstractions.Identity;
using NovaCore.AspNetCore.Auth;
using NovaCore.AspNetCore.Authorization;
using OpenIddict.Abstractions;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// The account API under <c>/api/v1/auth</c> (the Identity module's paths): self-service registration, email
/// confirmation and sending the confirmation again (anonymous; none of them reveals whether an address has an account),
/// and for the signed-in caller: who am I, change password, sign out everywhere, and authenticator-app two-factor
/// authentication.
/// </summary>
internal static class AccountEndpoints
{
    public const string ConfirmEmailPath = "api/v1/auth/confirm-email";

    private static readonly MessageResponse GenericAccepted =
        new("If the email address is new, a confirmation link has been sent.");

    private static readonly MessageResponse ConfirmationResent =
        new("If the address belongs to an account that isn't confirmed yet, a new confirmation link has been sent.");

    /// <summary>A message for the caller, e.g. after registration or when a resource is missing.</summary>
    internal sealed record MessageResponse(string Message);

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/v1/auth").WithTags("Auth").RequireRateLimiting("strict");

        auth.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new account (sends an email confirmation link)")
            .AllowAnonymous();

        auth.MapGet("/confirm-email", ConfirmEmail)
            .WithName("ConfirmEmail")
            .WithSummary("Confirm an email address using the token from the confirmation link")
            .AllowAnonymous();

        auth.MapPost("/resend-confirmation-email", ResendConfirmationEmail)
            .WithName("ResendConfirmationEmail")
            .WithSummary("Send the email confirmation link again (only to an account that isn't confirmed yet)")
            .ProducesValidationProblem()
            .AllowAnonymous();

        auth.MapGet("/me", Me)
            .WithName("Me").WithSummary("Current principal: id, email, roles, permissions")
            .RequireAuthorization();

        auth.MapPost("/logout-all", LogoutAll)
            .WithName("LogoutAll").WithSummary("Revoke every refresh token and sign-in session of the current user")
            .RequireAuthorization();

        auth.MapPost("/change-password", ChangePassword)
            .WithName("ChangePassword").WithSummary("Change password (revokes all sessions)")
            .RequireAuthorization();

        auth.MapPost("/2fa/setup", SetUpTwoFactor)
            .WithName("TwoFactorSetup").WithSummary("Get the authenticator shared key + otpauth URI (needs the password; 409 once 2FA is enabled)")
            .RequireAuthorization();

        auth.MapPost("/2fa/enable", EnableTwoFactor)
            .WithName("TwoFactorEnable")
            .WithSummary("Enable 2FA with the password and a TOTP code; returns new recovery codes, also when 2FA is already on (ends every session when it turns 2FA on)")
            .RequireAuthorization();

        auth.MapPost("/2fa/disable", DisableTwoFactor)
            .WithName("TwoFactorDisable").WithSummary("Disable 2FA with the password and a TOTP code (ends every session)")
            .RequireAuthorization();

        return auth;
    }

    private static async Task<Results<Ok<MessageResponse>, ValidationProblem>> Register(
        RegisterRequest request,
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        EmailQueue emails,
        SecurityAuditLogger audit,
        IOptions<JwtOptions> jwt,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            // An existing account gets the answer a new one gets: the response doesn't reveal it.
            if (result.Errors.All(error => error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)))
                return TypedResults.Ok(GenericAccepted);

            return PasswordProblem(result.Errors.Select(error => error.Description).ToArray());
        }

        // The seed creates the User role. Without it (seeding off) there are no roles yet, and the account starts without
        // one; the admin API grants roles later.
        if (await roles.RoleExistsAsync(IdentityRoles.User))
            await users.AddToRoleAsync(user, IdentityRoles.User);

        await SendConfirmationAsync(users, emails, jwt.Value, user, cancellationToken);

        audit.UserRegistered(user.Id);
        return TypedResults.Ok(GenericAccepted);
    }

    // Identity's /resendConfirmationEmail sends to a confirmed account too; here only an unconfirmed one gets a link, so
    // nobody can have emails sent to a confirmed address. The answer is the same either way (RESEARCH R-084).
    private static async Task<Ok<MessageResponse>> ResendConfirmationEmail(
        ResendConfirmationEmailRequest request,
        UserManager<ApplicationUser> users,
        EmailQueue emails,
        IOptions<JwtOptions> jwt,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is not null && !await users.IsEmailConfirmedAsync(user))
            await SendConfirmationAsync(users, emails, jwt.Value, user, cancellationToken);

        return TypedResults.Ok(ConfirmationResent);
    }

    private static async Task SendConfirmationAsync(
        UserManager<ApplicationUser> users, EmailQueue emails, JwtOptions jwt, ApplicationUser user, CancellationToken cancellationToken)
    {
        var link = EmailLinks.Create(jwt, ConfirmEmailPath, user.Id, await users.GenerateEmailConfirmationTokenAsync(user));

        // Queued, not sent here: the answer mustn't wait for the mail server, or its timing would reveal the account.
        await emails.EnqueueAsync(user.Email!, "Confirm your account",
            $"Welcome! Confirm your email address: <a href=\"{link}\">Confirm</a>", cancellationToken);
    }

    internal sealed record MeResponse(
        string UserId, string? UserName, string? Email, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);

    // The permissions are the ones the core resolved on the server for this request, not the token's.
    private static Results<Ok<MeResponse>, UnauthorizedHttpResult> Me(ICurrentUser currentUser) =>
        currentUser is { IsAuthenticated: true, UserId: { } userId }
            ? TypedResults.Ok(new MeResponse(userId, currentUser.UserName, currentUser.Email, currentUser.Roles, currentUser.Permissions))
            : TypedResults.Unauthorized();

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> LogoutAll(
        UserManager<ApplicationUser> users,
        ICurrentUser currentUser,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        SecurityAuditLogger audit,
        CancellationToken cancellationToken)
    {
        var user = await FindAsync(users, currentUser);
        if (user is null)
            return TypedResults.Unauthorized();

        // A new security stamp ends the provider's sign-in cookie on its next use.
        await users.UpdateSecurityStampAsync(user);
        await RevokeSessionsAsync(user, tokens, authorizations, cancellationToken);
        audit.SessionsRevoked(user.Id, "logout-all");
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, ValidationProblem, UnauthorizedHttpResult>> ChangePassword(
        ChangePasswordRequest request,
        UserManager<ApplicationUser> users,
        ICurrentUser currentUser,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        SecurityAuditLogger audit,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var user = await FindAsync(users, currentUser);
        if (user is null)
            return TypedResults.Unauthorized();

        // Like the sign-in page: a locked account changes nothing and a wrong current password counts toward lockout, so a
        // stolen access token can't guess the password more often than the sign-in page allows. ChangePasswordAsync checks
        // the password itself, so it is hashed once.
        if (await users.IsLockedOutAsync(user))
            return PasswordProblem(["The account is locked. Try again later."]);

        // ChangePasswordAsync also renews the security stamp, which ends the provider's sign-in cookie.
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
            {
                await users.AccessFailedAsync(user);
                if (await users.IsLockedOutAsync(user))
                    audit.AccountLockedOut(user.Id, context.Connection.RemoteIpAddress?.ToString());
            }

            return PasswordProblem(result.Errors.Select(error => error.Description).ToArray());
        }

        await users.ResetAccessFailedCountAsync(user);

        // Defence in depth: a password change ends every session.
        await RevokeSessionsAsync(user, tokens, authorizations, cancellationToken);
        audit.PasswordChanged(user.Id);
        audit.SessionsRevoked(user.Id, "password-changed");
        return TypedResults.NoContent();
    }

    // Refresh tokens and authorizations can be revoked; issued access tokens stay valid until they expire (15 minutes).
    internal static async Task RevokeSessionsAsync(
        ApplicationUser user,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        CancellationToken cancellationToken)
    {
        var subject = user.Id.ToString();
        await tokens.RevokeBySubjectAsync(subject, cancellationToken);
        await authorizations.RevokeBySubjectAsync(subject, cancellationToken);
    }

    internal sealed record TwoFactorSetupResponse(string SharedKey, string AuthenticatorUri);

    internal sealed record RecoveryCodesResponse(string[] RecoveryCodes);

    private static async Task<Results<Ok<TwoFactorSetupResponse>, Conflict<MessageResponse>, ValidationProblem, UnauthorizedHttpResult>> SetUpTwoFactor(
        TwoFactorSetupRequest request,
        UserManager<ApplicationUser> users,
        ICurrentUser currentUser,
        IHostEnvironment environment,
        SecurityAuditLogger audit,
        HttpContext context)
    {
        var user = await FindAsync(users, currentUser);
        if (user is null)
            return TypedResults.Unauthorized();

        if (await ReauthenticateAsync(users, user, request.Password, audit, context) is { } wrongPassword)
            return wrongPassword;

        // An enabled authenticator's key stays secret: with it, any holder of an access token could compute codes and
        // turn two-factor authentication off. A new authenticator starts after disabling (which takes a code).
        if (await users.GetTwoFactorEnabledAsync(user))
            return TypedResults.Conflict(new MessageResponse("Two-factor authentication is already enabled. Disable it to set up a new authenticator."));

        var key = await users.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await users.ResetAuthenticatorKeyAsync(user);
            key = await users.GetAuthenticatorKeyAsync(user);
        }

        // The authenticator app shows the host application's name next to the account.
        var issuer = Uri.EscapeDataString(environment.ApplicationName);
        var account = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id.ToString());
        var uri = $"otpauth://totp/{issuer}:{account}?secret={key}&issuer={issuer}&digits=6&period=30";

        return TypedResults.Ok(new TwoFactorSetupResponse(key!, uri));
    }

    // Also the way to new recovery codes while two-factor authentication is on: the same proof, no endpoint of its own.
    private static async Task<Results<Ok<RecoveryCodesResponse>, ValidationProblem, UnauthorizedHttpResult>> EnableTwoFactor(
        TwoFactorCodeRequest request,
        UserManager<ApplicationUser> users,
        ICurrentUser currentUser,
        HybridCache cache,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        SecurityAuditLogger audit,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var user = await FindAsync(users, currentUser);
        if (user is null)
            return TypedResults.Unauthorized();

        if (await VerifyPasswordAndCodeAsync(users, user, request, audit, context) is { } problem)
            return problem;

        if (await users.GetTwoFactorEnabledAsync(user))
        {
            audit.RecoveryCodesRenewed(user.Id);
        }
        else
        {
            // The sessions so far began without the second factor (a stolen refresh token among them), so they end.
            await users.SetTwoFactorEnabledAsync(user, true);
            await RevokeSessionsAsync(user, tokens, authorizations, cancellationToken);

            // An administrator's permissions depend on it (IdentityPermissionResolver).
            await cache.RemovePermissionsAsync(user.Id.ToString(), cancellationToken);
            audit.TwoFactorEnabled(user.Id);
            audit.SessionsRevoked(user.Id, "two-factor-enabled");
        }

        var recoveryCodes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return TypedResults.Ok(new RecoveryCodesResponse(recoveryCodes?.ToArray() ?? []));
    }

    private static async Task<Results<NoContent, ValidationProblem, UnauthorizedHttpResult>> DisableTwoFactor(
        TwoFactorCodeRequest request,
        UserManager<ApplicationUser> users,
        ICurrentUser currentUser,
        HybridCache cache,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        SecurityAuditLogger audit,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var user = await FindAsync(users, currentUser);
        if (user is null)
            return TypedResults.Unauthorized();

        if (await VerifyPasswordAndCodeAsync(users, user, request, audit, context) is { } problem)
            return problem;

        await users.SetTwoFactorEnabledAsync(user, false);
        await users.ResetAuthenticatorKeyAsync(user);
        await RevokeSessionsAsync(user, tokens, authorizations, cancellationToken);
        await cache.RemovePermissionsAsync(user.Id.ToString(), cancellationToken);
        audit.TwoFactorDisabled(user.Id);
        audit.SessionsRevoked(user.Id, "two-factor-disabled");

        return TypedResults.NoContent();
    }

    // Re-authentication for the two-factor endpoints (ASVS 7.5.1): with an access token alone, nobody sets up, replaces
    // or removes the second factor. As on the sign-in page, a locked account is refused and a wrong password counts
    // toward lockout, so a stolen access token can't guess the password more often there.
    private static async Task<ValidationProblem?> ReauthenticateAsync(
        UserManager<ApplicationUser> users, ApplicationUser user, string password, SecurityAuditLogger audit, HttpContext context)
    {
        if (await users.IsLockedOutAsync(user))
            return PasswordProblem(["The account is locked. Try again later."]);

        if (await users.CheckPasswordAsync(user, password))
            return null;

        await CountFailureAsync(users, user, audit, context);
        return PasswordProblem(["The password is incorrect."]);
    }

    // The sign-in page's rules for the code too: a wrong one counts toward lockout, and only full success resets the count.
    private static async Task<ValidationProblem?> VerifyPasswordAndCodeAsync(
        UserManager<ApplicationUser> users, ApplicationUser user, TwoFactorCodeRequest request, SecurityAuditLogger audit, HttpContext context)
    {
        if (await ReauthenticateAsync(users, user, request.Password, audit, context) is { } wrongPassword)
            return wrongPassword;

        if (!await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, request.Code))
        {
            await CountFailureAsync(users, user, audit, context);
            return InvalidCode();
        }

        await users.ResetAccessFailedCountAsync(user);
        return null;
    }

    private static async Task CountFailureAsync(UserManager<ApplicationUser> users, ApplicationUser user, SecurityAuditLogger audit, HttpContext context)
    {
        await users.AccessFailedAsync(user);
        if (await users.IsLockedOutAsync(user))
            audit.AccountLockedOut(user.Id, context.Connection.RemoteIpAddress?.ToString());
    }

    private static Task<ApplicationUser?> FindAsync(UserManager<ApplicationUser> users, ICurrentUser currentUser) =>
        Guid.TryParse(currentUser.UserId, out var id) ? users.FindByIdAsync(id.ToString()) : Task.FromResult<ApplicationUser?>(null);

    private static ValidationProblem InvalidCode() => TypedResults.ValidationProblem(new Dictionary<string, string[]>
    {
        ["code"] = ["The verification code is invalid."]
    });

    private static ValidationProblem PasswordProblem(string[] errors) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["password"] = errors });

    private static async Task<Results<Ok<MessageResponse>, BadRequest<MessageResponse>>> ConfirmEmail(
        Guid userId,
        string token,
        UserManager<ApplicationUser> users,
        SecurityAuditLogger audit)
    {
        var invalid = TypedResults.BadRequest(new MessageResponse("Invalid or expired confirmation token."));

        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || EmailLinks.DecodeToken(token) is not { } decoded)
            return invalid;

        if (!(await users.ConfirmEmailAsync(user, decoded)).Succeeded)
            return invalid;

        audit.EmailConfirmed(user.Id);
        return TypedResults.Ok(new MessageResponse("Email confirmed. You can now sign in."));
    }
}
