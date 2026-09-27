using System.ComponentModel.DataAnnotations;

namespace NovaCore.Identity.OpenIddict;

// The request bodies of the account API. They are public because .NET 10's validation source generator only covers
// public types; an internal request would reach its handler unvalidated (docs/plan/RESEARCH.md R-062).

/// <summary>The body of <c>POST /api/v1/auth/register</c>.</summary>
/// <param name="Email">The account's email address, also its user name.</param>
/// <param name="Password">12 to 128 characters; length is the only rule (OWASP).</param>
/// <param name="DisplayName">An optional name to show instead of the email address.</param>
public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, Length(12, 128)] string Password,
    [MaxLength(128)] string? DisplayName);

/// <summary>The body of <c>POST /api/v1/auth/2fa/enable</c> and <c>/2fa/disable</c>.</summary>
/// <param name="Code">The current code from the authenticator app.</param>
public sealed record TwoFactorCodeRequest([Required, Length(6, 8)] string Code);
