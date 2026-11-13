using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace NovaCore.Identity.OpenIddict;

/// <summary>What redeeming a reset code decided; the endpoint turns it into an answer.</summary>
internal enum ResetCodeOutcome
{
    Succeeded,

    // No code, a wrong one, an expired one, a used one, or too many wrong ones on the account: one outcome, so the
    // answer doesn't tell them apart (and an unknown or unconfirmed account answers the same, at the endpoint).
    InvalidCode,

    // The code is right but the new password breaks the policy, or its save lost to a concurrent write of the account;
    // the code stays good for another try.
    PasswordRejected
}

/// <summary>
/// Password reset with a short code typed into a project's own screen (docs/plan/DECISIONS.md D-091 (4)), next to the
/// emailed link of <see cref="PasswordResetPage"/>. Identity's TOTP providers can neither invalidate one code nor count
/// tries (R-137), so the code is ours: 8 random digits, stored as a <see cref="PasswordHasher{TUser}"/> hash with its
/// expiry and wrong tries in a user token, valid 15 minutes, once, for 5 wrong tries. A new code replaces the old one but
/// not the account's run of wrong tries; at 100 (NIST SP 800-63B-4 §3.2.2) codes stop working until a sign-in or a link
/// reset succeeds. A wrong code never counts toward Identity's lockout (OWASP Forgot Password: no lockout from a reset).
/// Every try is counted, on the code and in the account's run, in one save before its code is compared; a try whose save
/// is lost to a concurrent request isn't compared and ends the code (fails closed), so racing guesses get no more tries,
/// and no more uncounted ones, than sequential ones.
/// </summary>
internal sealed class PasswordResetCodes(
    UserManager<ApplicationUser> users,
    IdentityServerDbContext db,
    [FromKeyedServices(PasswordResetCodes.HasherKey)] IPasswordHasher<ApplicationUser> hasher,
    DummyPasswordHash dummyHash,
    TimeProvider timeProvider)
{
    public const int Digits = 8;
    public const int MaxTriesPerCode = 5;
    public const int MaxConsecutiveFailures = 100;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    internal const string LoginProvider = "[NovaCore]";
    private const string CodeToken = "PasswordResetCode";
    internal const string FailuresToken = "PasswordResetCodeFailures";

    /// <summary>
    /// The codes' hasher in DI: a <see cref="PasswordHasher{TUser}"/> with its defaults, like the recovery codes' (D-053
    /// (3)), not the host's 600,000 iterations, since a short code with many tries costs a hash each (D-091 (4)), so not
    /// the host's <see cref="IPasswordHasher{TUser}"/> either. It is in DI only so a test can count its work (every
    /// address costs one hash, V8-3); a field would hash the same.
    /// </summary>
    internal const string HasherKey = "NovaCore.PasswordResetCodes";

    /// <summary>Makes a new code for <paramref name="user"/>, replacing an unused one, and returns it for the email.</summary>
    public async Task<string> IssueAsync(ApplicationUser user)
    {
        var code = NewCode();
        await SaveCodeAsync(user, new StoredCode(timeProvider.GetUtcNow() + Lifetime, Tries: 0, hasher.HashPassword(user, code)));
        return code;
    }

    /// <summary>
    /// The hash <see cref="IssueAsync"/> makes, for an address that gets no code (unknown or unconfirmed), so asking for a
    /// code costs every address the same (D-030 (1), D-091 (4)). Nothing is saved.
    /// </summary>
    public void IssueToNobody() => hasher.HashPassword(new ApplicationUser { CreatedAtUtc = default }, NewCode());

    /// <summary>
    /// The hash check <see cref="RedeemAsync"/> makes, for an address that has no account to reset (unknown or
    /// unconfirmed): the typed code is checked against a dummy hash of the codes' hasher.
    /// </summary>
    public void RedeemForNobody(string code) => dummyHash.CheckCode(hasher, code);

    /// <summary>
    /// Sets <paramref name="newPassword"/> when <paramref name="code"/> is the account's current code. A try is counted
    /// (the code's tries and the account's run, one save) before the code is compared, and only a counted try is
    /// compared: of requests racing with one code, the ones whose count can't be saved are refused and end the code. The code is used up before the password changes, with the
    /// user's concurrency stamp, so two requests with one code change it once; the code's end and the new password commit
    /// together, so a reset whose password save fails leaves the code usable.
    /// </summary>
    public async Task<(ResetCodeOutcome Outcome, IReadOnlyList<string> Errors)> RedeemAsync(ApplicationUser user, string code, string newPassword)
    {
        // Every refusal before the comparison checks the dummy hash instead: one hash check for every answer.
        var failures = await FailuresAsync(user);
        if (failures >= MaxConsecutiveFailures
            || await ReadCodeAsync(user) is not { } stored)
        {
            RedeemForNobody(code);
            return (ResetCodeOutcome.InvalidCode, []);
        }

        if (stored.ExpiresAt <= timeProvider.GetUtcNow() || stored.Tries >= MaxTriesPerCode)
        {
            await EndCodeAsync(user);
            RedeemForNobody(code);
            return (ResetCodeOutcome.InvalidCode, []);
        }

        // Counted first, the code's tries and the account's run in one save: it carries the user's concurrency stamp, so
        // of requests that read the account before either wrote (another guess, a new code) only one saves. The other
        // guesses aren't compared at all, and the code ends, so no compared try goes uncounted (V8-2, V8-9).
        var counted = stored with { Tries = stored.Tries + 1 };
        if (!await SaveCountAsync(user, counted, failures + 1))
        {
            await EndCodeAsync(user);
            RedeemForNobody(code);
            return (ResetCodeOutcome.InvalidCode, []);
        }

        if (hasher.VerifyHashedPassword(user, stored.Hash, code) == PasswordVerificationResult.Failed)
        {
            // Counted already; the fifth wrong try also ends the code (a later read refuses it either way).
            if (counted.Tries >= MaxTriesPerCode)
                await EndCodeAsync(user);
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
        {
            // The right code isn't a wrong try: both counts go back. If another request saved in between, this save
            // fails and the try stays counted, the stricter side.
            await SaveCountAsync(user, stored, failures);
            return (ResetCodeOutcome.PasswordRejected, errors);
        }

        // A transaction of our own runs inside the execution strategy, as EF Core requires (retries: UseUpAndResetAsync).
        var strategy = db.Database.CreateExecutionStrategy();
        var redeemed = await strategy.ExecuteAsync(() => UseUpAndResetAsync(user, newPassword));
        if (redeemed.Outcome == ResetCodeOutcome.Succeeded)
            await ClearFailuresAsync(user);
        return redeemed;
    }

    // The code's end and the new password commit together: a reset that fails leaves the code as it was (V8-6).
    // A retrying strategy's replay can't succeed: Identity's saves accept their changes, so after a rolled-back first
    // attempt the tracked user carries the stamp that attempt wrote, and the replay fails on the concurrency stamp. That
    // is the safe side (nothing is applied twice); the caller gets a refusal and asks again (R-139).
    private async Task<(ResetCodeOutcome Outcome, IReadOnlyList<string> Errors)> UseUpAndResetAsync(ApplicationUser user, string newPassword)
    {
        // Disposed without a commit, it rolls back.
        await using var transaction = await db.Database.BeginTransactionAsync();

        // Used up first: the save carries the concurrency stamp, so the loser of a race with the same code fails here.
        if (!(await users.RemoveAuthenticationTokenAsync(user, LoginProvider, CodeToken)).Succeeded)
            return (ResetCodeOutcome.InvalidCode, []);

        // Identity's own reset (a fresh token from this process): it hashes the password and renews the security stamp,
        // which ends the provider's sign-in cookie.
        var reset = await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), newPassword);
        if (!reset.Succeeded)
            return (ResetCodeOutcome.PasswordRejected, reset.Errors.Select(error => error.Description).ToList());

        await transaction.CommitAsync();
        return (ResetCodeOutcome.Succeeded, []);
    }

    /// <summary>Ends the account's run of wrong codes: a sign-in or a link reset succeeded, so its owner is back.</summary>
    public async Task ClearFailuresAsync(ApplicationUser user)
    {
        if (await users.GetAuthenticationTokenAsync(user, LoginProvider, FailuresToken) is not null)
            await users.RemoveAuthenticationTokenAsync(user, LoginProvider, FailuresToken);
    }

    // Both tokens in one save: UserManager's SetAuthenticationTokenAsync saves the user once per token, so two calls are
    // two stamp checks, and the second can lose after the first won. The store's token methods only track the change;
    // UpdateAsync saves it with the user's concurrency stamp. A run back at zero keeps its token at "0".
    private async Task<bool> SaveCountAsync(ApplicationUser user, StoredCode code, int failures)
    {
        await TrackTokenAsync(user, CodeToken, Format(code));
        await TrackTokenAsync(user, FailuresToken, failures.ToString(CultureInfo.InvariantCulture));
        return (await users.UpdateAsync(user)).Succeeded;
    }

    private async Task TrackTokenAsync(ApplicationUser user, string name, string value)
    {
        if (await db.UserTokens.FindAsync(user.Id, LoginProvider, name) is { } token)
            token.Value = value;
        else
            db.UserTokens.Add(new IdentityUserToken<Guid> { UserId = user.Id, LoginProvider = LoginProvider, Name = name, Value = value });
    }

    // Deletes the code in the database, apart from the change tracker: after a save lost to the concurrency stamp, the
    // context still holds that save's changes, and every later save through UserManager would fail the same way.
    private async Task EndCodeAsync(ApplicationUser user) =>
        await db.UserTokens
            .Where(token => token.UserId == user.Id && token.LoginProvider == LoginProvider && token.Name == CodeToken)
            .ExecuteDeleteAsync();

    private static string NewCode() =>
        RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8", CultureInfo.InvariantCulture);

    private async Task<int> FailuresAsync(ApplicationUser user) =>
        int.TryParse(await users.GetAuthenticationTokenAsync(user, LoginProvider, FailuresToken), NumberStyles.None, CultureInfo.InvariantCulture, out var failures)
            ? failures
            : 0;

    private Task<IdentityResult> SaveCodeAsync(ApplicationUser user, StoredCode code) =>
        users.SetAuthenticationTokenAsync(user, LoginProvider, CodeToken, Format(code));

    private static string Format(StoredCode code) =>
        string.Join('|',
            code.ExpiresAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            code.Tries.ToString(CultureInfo.InvariantCulture),
            code.Hash);

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
