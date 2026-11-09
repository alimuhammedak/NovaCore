using System.ComponentModel.DataAnnotations;

namespace NovaCore.Identity.OpenIddict;

// The request bodies of the account API. They are public because .NET 10's validation source generator only covers
// public types; an internal request would reach its handler unvalidated (docs/plan/RESEARCH.md R-062).

/// <summary>The body of <c>POST /api/v1/auth/register</c>.</summary>
/// <param name="Email">The account's email address, also its user name.</param>
/// <param name="Password">15 to 128 characters and not a commonly used password; no composition rules (NIST SP 800-63B-4).</param>
/// <param name="DisplayName">An optional name to show instead of the email address.</param>
public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, Length(15, 128)] string Password,
    [MaxLength(128)] string? DisplayName);

/// <summary>The body of <c>POST /api/v1/auth/resend-confirmation-email</c>.</summary>
/// <param name="Email">The address the account registered with.</param>
public sealed record ResendConfirmationEmailRequest([Required, EmailAddress, MaxLength(256)] string Email);

/// <summary>The body of <c>POST /api/v1/auth/2fa/setup</c>.</summary>
/// <param name="Password">The account's current password: an access token alone doesn't change the second factor.</param>
public sealed record TwoFactorSetupRequest([Required] string Password);

/// <summary>The body of <c>POST /api/v1/auth/2fa/enable</c> and <c>/2fa/disable</c>.</summary>
/// <param name="Password">The account's current password: an access token alone doesn't change the second factor.</param>
/// <param name="Code">The current code from the authenticator app. Each code is accepted once.</param>
public sealed record TwoFactorCodeRequest([Required] string Password, [Required, Length(6, 8)] string Code);

/// <summary>The body of <c>POST /api/v1/auth/change-password</c>.</summary>
/// <param name="CurrentPassword">The password the caller signs in with now.</param>
/// <param name="NewPassword">15 to 128 characters and not a commonly used password; no composition rules (NIST SP 800-63B-4).</param>
public sealed record ChangePasswordRequest([Required] string CurrentPassword, [Required, Length(15, 128)] string NewPassword);

/// <summary>The body of <c>POST /api/v1/admin/users/{userId}/roles</c>.</summary>
/// <param name="Role">The name of an existing role.</param>
public sealed record AssignRoleRequest([Required, MaxLength(256)] string Role);

/// <summary>The body of <c>POST /api/v1/auth/session/login</c>: the password step of a project's own sign-in screen.</summary>
/// <param name="Email">The account's email address.</param>
/// <param name="Password">The account's password.</param>
/// <param name="ReturnUrl">Where the screen goes after signing in, usually the authorization request that sent it there. A path on this host only.</param>
public sealed record SessionSignInRequest(
    [Required, MaxLength(256)] string Email,
    [Required, MaxLength(1024)] string Password,
    [MaxLength(4096), LocalUrl] string? ReturnUrl);

/// <summary>The body of <c>POST /api/v1/auth/session/login/2fa</c>: the code step, after the password step asked for it.</summary>
/// <param name="Code">The current code from the authenticator app, or one of the recovery codes. Each is accepted once.</param>
/// <param name="ReturnUrl">Where the screen goes after signing in. A path on this host only.</param>
public sealed record SessionCodeRequest(
    [Required, MaxLength(64)] string Code,
    [MaxLength(4096), LocalUrl] string? ReturnUrl);
