using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace NovaCore.Identity.OpenIddict;

/// <summary>What redeeming a reset code decided; the endpoint turns it into an answer.</summary>
internal enum ResetCodeOutcome
{
    Succeeded,

    // No code, a wrong one, an expired one, a used one, or too many wrong ones on the account: one outcome, so the
    // answer doesn't tell them apart (and an unknown or unconfirmed account answers the same, at the endpoint).
    InvalidCode,

    // The code is right but the new password breaks the policy; the code stays good for another try.
    PasswordRejected
}

/// <summary>
/// Password reset with a short code typed into a project's own screen (docs/plan/DECISIONS.md D-091 (4)), next to the
/// emailed link of <see cref="PasswordResetPage"/>. Identity's TOTP providers can neither invalidate one code nor count
/// tries (R-137), so the code is ours: 8 random digits, stored as a <see cref="PasswordHasher{TUser}"/> hash with its
/// expiry and wrong tries in a user token, valid 15 minutes, once, for 5 wrong tries. A new code replaces the old one but
/// not the account's run of wrong tries; at 100 (NIST SP 800-63B-4 §3.2.2) codes stop working until a sign-in or a link
/// reset succeeds. A wrong code never counts toward Identity's lockout (OWASP Forgot Password: no lockout from a reset).
/// </summary>
internal sealed class PasswordResetCodes(UserManager<ApplicationUser> users, TimeProvider timeProvider)
{
    public const int Digits = 8;
    public const int MaxTriesPerCode = 5;
    public const int MaxConsecutiveFailures = 100;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    internal const string LoginProvider = "[NovaCore]";
    private const string CodeToken = "PasswordResetCode";
    internal const string FailuresToken = "PasswordResetCodeFailures";

    // The recovery codes' hasher and defaults (D-053 (3)): a short code, many tries cost a hash each.
    private readonly PasswordHasher<ApplicationUser> _hasher = new();

    /// <summary>Makes a new code for <paramref name="user"/>, replacing an unused one, and returns it for the email.</summary>
    public async Task<string> IssueAsync(ApplicationUser user)
    {
        var code = RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8", CultureInfo.InvariantCulture);
        await SaveCodeAsync(user, new StoredCode(timeProvider.GetUtcNow() + Lifetime, Tries: 0, _hasher.HashPassword(user, code)));
        return code;
    }

    /// <summary>
    /// Sets <paramref name="newPassword"/> when <paramref name="code"/> is the account's current code. The code is used up
    /// before the password changes, with the user's concurrency stamp, so two requests with one code change it once.
    /// </summary>
    public async Task<(ResetCodeOutcome Outcome, IReadOnlyList<string> Errors)> RedeemAsync(ApplicationUser user, string code, string newPassword)
    {
        if (await FailuresAsync(user) >= MaxConsecutiveFailures
            || await ReadCodeAsync(user) is not { } stored)
            return (ResetCodeOutcome.InvalidCode, []);

        if (stored.ExpiresAt <= timeProvider.GetUtcNow())
        {
            await users.RemoveAuthenticationTokenAsync(user, LoginProvider, CodeToken);
            return (ResetCodeOutcome.InvalidCode, []);
        }

        if (_hasher.VerifyHashedPassword(user, stored.Hash, code) == PasswordVerificationResult.Failed)
        {
            await CountWrongCodeAsync(user, stored);
            return (ResetCodeOutcome.InvalidCode, []);
        }

        // The policy first, so a rejected password leaves the right code usable, like the link (PasswordResetPage).
        var errors = new List<string>();
        foreach (var validator in users.PasswordValidators)
        {
            var check = await validator.ValidateAsync(users, user, newPassword);
            errors.AddRange(check.Errors.Select(error => error.Description));
        }

        if (errors.Count > 0)
            return (ResetCodeOutcome.PasswordRejected, errors);

        // Used up first: the save carries the concurrency stamp, so the loser of a race with the same code fails here.
        if (!(await users.RemoveAuthenticationTokenAsync(user, LoginProvider, CodeToken)).Succeeded)
            return (ResetCodeOutcome.InvalidCode, []);

        // Identity's own reset (a fresh token from this process): it hashes the password and renews the security stamp,
        // which ends the provider's sign-in cookie.
        var reset = await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), newPassword);
        if (!reset.Succeeded)
            return (ResetCodeOutcome.PasswordRejected, reset.Errors.Select(error => error.Description).ToList());

        await ClearFailuresAsync(user);
        return (ResetCodeOutcome.Succeeded, []);
    }

    /// <summary>Ends the account's run of wrong codes: a sign-in or a link reset succeeded, so its owner is back.</summary>
    public async Task ClearFailuresAsync(ApplicationUser user)
    {
        if (await users.GetAuthenticationTokenAsync(user, LoginProvider, FailuresToken) is not null)
            await users.RemoveAuthenticationTokenAsync(user, LoginProvider, FailuresToken);
    }

    private async Task CountWrongCodeAsync(ApplicationUser user, StoredCode stored)
    {
        var failures = await FailuresAsync(user) + 1;
        await users.SetAuthenticationTokenAsync(user, LoginProvider, FailuresToken, failures.ToString(CultureInfo.InvariantCulture));

        if (stored.Tries + 1 >= MaxTriesPerCode)
            await users.RemoveAuthenticationTokenAsync(user, LoginProvider, CodeToken);
        else
            await SaveCodeAsync(user, stored with { Tries = stored.Tries + 1 });
    }

    private async Task<int> FailuresAsync(ApplicationUser user) =>
        int.TryParse(await users.GetAuthenticationTokenAsync(user, LoginProvider, FailuresToken), NumberStyles.None, CultureInfo.InvariantCulture, out var failures)
            ? failures
            : 0;

    private Task<IdentityResult> SaveCodeAsync(ApplicationUser user, StoredCode code) =>
        users.SetAuthenticationTokenAsync(user, LoginProvider, CodeToken, string.Join('|',
            code.ExpiresAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            code.Tries.ToString(CultureInfo.InvariantCulture),
            code.Hash));

    private async Task<StoredCode?> ReadCodeAsync(ApplicationUser user)
    {
        var parts = (await users.GetAuthenticationTokenAsync(user, LoginProvider, CodeToken))?.Split('|');
        return parts is [var expires, var tries, var hash]
               && long.TryParse(expires, NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAt)
               && int.TryParse(tries, NumberStyles.None, CultureInfo.InvariantCulture, out var triesSoFar)
            ? new StoredCode(DateTimeOffset.FromUnixTimeMilliseconds(expiresAt), triesSoFar, hash)
            : null;
    }

    private sealed record StoredCode(DateTimeOffset ExpiresAt, int Tries, string Hash);
}
