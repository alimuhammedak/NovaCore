using System.Buffers.Text;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NovaCore.Abstractions.Identity;
using NovaCore.AspNetCore.Auth;
using OpenIddict.Abstractions;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// The account API under <c>/api/v1/auth</c> (the Identity module's paths): self-service registration and email
/// confirmation (anonymous; registering an existing address looks like registering a new one), and for the signed-in
/// caller: who am I, change password, sign out everywhere, and authenticator-app two-factor authentication.
/// </summary>
internal static class AccountEndpoints
{
    public const string ConfirmEmailPath = "api/v1/auth/confirm-email";

    private static readonly object GenericAccepted =
        new { message = "If the email address is new, a confirmation link has been sent." };

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
            .WithName("TwoFactorSetup").WithSummary("Get the authenticator shared key + otpauth URI")
            .RequireAuthorization();

        auth.MapPost("/2fa/enable", EnableTwoFactor)
            .WithName("TwoFactorEnable").WithSummary("Enable 2FA by verifying a TOTP code (returns recovery codes)")
            .RequireAuthorization();

        auth.MapPost("/2fa/disable", DisableTwoFactor)
            .WithName("TwoFactorDisable").WithSummary("Disable 2FA after verifying a TOTP code")
            .RequireAuthorization();

        return auth;
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        UserManager<ApplicationUser> users,
        IEmailSender emailSender,
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
                return Results.Ok(GenericAccepted);

            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["password"] = result.Errors.Select(error => error.Description).ToArray()
            });
        }

        await users.AddToRoleAsync(user, IdentityRoles.User);

        var token = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(await users.GenerateEmailConfirmationTokenAsync(user)));

        // The link starts from the configured public URL, never from the request's Host header (host header poisoning).
        var link = QueryHelpers.AddQueryString(
            new Uri(new Uri(jwt.Value.Authority.TrimEnd('/') + "/"), ConfirmEmailPath).AbsoluteUri,
            new Dictionary<string, string?> { ["userId"] = user.Id.ToString(), ["token"] = token });

        await emailSender.SendAsync(request.Email, "Confirm your account",
            $"Welcome! Confirm your email address: <a href=\"{link}\">Confirm</a>", cancellationToken);

        audit.UserRegistered(user.Id, request.Email);
        return Results.Ok(GenericAccepted);
    }

    internal sealed record MeResponse(
        string UserId, string? UserName, string? Email, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);

    // The permissions are the ones the core resolved on the server for this request, not the token's.
    private static IResult Me(ICurrentUser currentUser) =>
        currentUser is { IsAuthenticated: true, UserId: { } userId }
            ? Results.Ok(new MeResponse(userId, currentUser.UserName, currentUser.Email, currentUser.Roles, currentUser.Permissions))
            : Results.Unauthorized();

    private static async Task<IResult> LogoutAll(
        UserManager<ApplicationUser> users,
        ICurrentUser currentUser,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        SecurityAuditLogger audit,
        CancellationToken cancellationToken)
    {
        var user = await FindAsync(users, currentUser);
        if (user is null)
            return Results.Unauthorized();

        // A new security stamp ends the provider's sign-in cookie on its next use.
        await users.UpdateSecurityStampAsync(user);
        await RevokeSessionsAsync(user, tokens, authorizations, cancellationToken);
        audit.SessionsRevoked(user.Id, "logout-all");
        return Results.NoContent();
    }

    private static async Task<IResult> ChangePassword(
        ChangePasswordRequest request,
        UserManager<ApplicationUser> users,
        ICurrentUser currentUser,
        IOpenIddictTokenManager tokens,
        IOpenIddictAuthorizationManager authorizations,
        SecurityAuditLogger audit,
        CancellationToken cancellationToken)
    {
        var user = await FindAsync(users, currentUser);
        if (user is null)
            return Results.Unauthorized();

        // ChangePasswordAsync also renews the security stamp, which ends the provider's sign-in cookie.
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["password"] = result.Errors.Select(error => error.Description).ToArray()
            });
        }

        // Defence in depth: a password change ends every session.
        await RevokeSessionsAsync(user, tokens, authorizations, cancellationToken);
        audit.PasswordChanged(user.Id);
        audit.SessionsRevoked(user.Id, "password-changed");
        return Results.NoContent();
    }

    // Refresh tokens and authorizations can be revoked; issued access tokens stay valid until they expire (15 minutes).
    private static async Task RevokeSessionsAsync(
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

    private static async Task<IResult> SetUpTwoFactor(
        UserManager<ApplicationUser> users, ICurrentUser currentUser, IHostEnvironment environment)
    {
        var user = await FindAsync(users, currentUser);
        if (user is null)
            return Results.Unauthorized();

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

        return Results.Ok(new TwoFactorSetupResponse(key!, uri));
    }

    private static async Task<IResult> EnableTwoFactor(
        TwoFactorCodeRequest request, UserManager<ApplicationUser> users, ICurrentUser currentUser, SecurityAuditLogger audit)
    {
        var user = await FindAsync(users, currentUser);
        if (user is null)
            return Results.Unauthorized();

        if (!await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, request.Code))
            return InvalidCode();

        await users.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await users.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        audit.TwoFactorEnabled(user.Id);

        return Results.Ok(new { recoveryCodes = recoveryCodes?.ToArray() ?? [] });
    }

    private static async Task<IResult> DisableTwoFactor(
        TwoFactorCodeRequest request, UserManager<ApplicationUser> users, ICurrentUser currentUser, SecurityAuditLogger audit)
    {
        var user = await FindAsync(users, currentUser);
        if (user is null)
            return Results.Unauthorized();

        if (!await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, request.Code))
            return InvalidCode();

        await users.SetTwoFactorEnabledAsync(user, false);
        await users.ResetAuthenticatorKeyAsync(user);
        audit.TwoFactorDisabled(user.Id);

        return Results.NoContent();
    }

    private static Task<ApplicationUser?> FindAsync(UserManager<ApplicationUser> users, ICurrentUser currentUser) =>
        Guid.TryParse(currentUser.UserId, out var id) ? users.FindByIdAsync(id.ToString()) : Task.FromResult<ApplicationUser?>(null);

    private static IResult InvalidCode() => Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["code"] = ["The verification code is invalid."]
    });

    private static async Task<IResult> ConfirmEmail(
        Guid userId,
        string token,
        UserManager<ApplicationUser> users,
        SecurityAuditLogger audit)
    {
        var invalid = Results.BadRequest(new { message = "Invalid or expired confirmation token." });

        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
            return invalid;

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token));
        }
        catch (FormatException)
        {
            return invalid;
        }

        if (!(await users.ConfirmEmailAsync(user, decoded)).Succeeded)
            return invalid;

        audit.EmailConfirmed(user.Id);
        return Results.Ok(new { message = "Email confirmed. You can now sign in." });
    }
}
