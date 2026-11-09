using Microsoft.AspNetCore.Identity;

namespace NovaCore.Identity.OpenIddict;

/// <summary>What a sign-in step decided; the caller turns it into a page or a JSON answer.</summary>
internal enum SignInOutcome
{
    Succeeded,
    TwoFactorRequired,

    // An unknown account, a wrong password and a locked account: one outcome, so no caller can tell them apart (D-030 (1)).
    InvalidCredentials,

    // Only after the right password (CheckUnconfirmedAsync), so it reveals nothing to someone who doesn't know it.
    EmailNotConfirmed,

    InvalidAuthenticatorCode,
    InvalidRecoveryCode,

    // The code step came without the two-factor cookie the password step leaves.
    NoTwoFactorSession
}

/// <summary>
/// The sign-in rules, in one place for every surface that signs a browser in (the HTML pages, the JSON endpoints, D-090
/// (2)): lockout, the unconfirmed account, the dummy hash for an unknown account, the two-factor step with an
/// authenticator or a recovery code, and the audit log. The caller reads the input and renders the outcome.
/// </summary>
internal sealed class SignInFlow(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    IPasswordHasher<ApplicationUser> hasher,
    DummyPasswordHash dummyHash,
    SecurityAuditLogger audit)
{
    /// <summary>The password step. On <see cref="SignInOutcome.TwoFactorRequired"/> the two-factor cookie names the user.</summary>
    public async Task<SignInOutcome> PasswordAsync(string? email, string? password, string? ip)
    {
        var user = string.IsNullOrWhiteSpace(email) ? null : await users.FindByEmailAsync(email);
        if (user is null || string.IsNullOrEmpty(password))
        {
            // An unknown address costs a password hash too, so the answer doesn't come sooner than for a known one.
            if (user is null && !string.IsNullOrEmpty(password))
                dummyHash.Check(hasher, password);

            audit.LoginFailed(null, ip, "unknown-user");
            return SignInOutcome.InvalidCredentials;
        }

        var result = await signIn.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
        if (result.IsNotAllowed)
            result = await CheckUnconfirmedAsync(user, password);

        if (result.Succeeded)
        {
            audit.LoginSucceeded(user.Id, ip);
            return SignInOutcome.Succeeded;
        }

        if (result.RequiresTwoFactor)
        {
            // SignInManager remembers the user in the two-factor cookie until the code step.
            audit.TwoFactorChallenged(user.Id);
            return SignInOutcome.TwoFactorRequired;
        }

        if (result.IsLockedOut)
            audit.AccountLockedOut(user.Id, ip);
        else
            audit.LoginFailed(user.Id, ip, result.IsNotAllowed ? "email-not-confirmed" : "bad-password");

        return result.IsNotAllowed ? SignInOutcome.EmailNotConfirmed : SignInOutcome.InvalidCredentials;
    }

    /// <summary>The code step: an authenticator code or one of the recovery codes, after the password step.</summary>
    public async Task<SignInOutcome> CodeAsync(string? code, string? ip)
    {
        // Only after the password step: the two-factor cookie names the user.
        var user = await signIn.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
            return SignInOutcome.NoTwoFactorSession;

        // An authenticator code is digits, even mistyped; Identity's recovery codes (XXXXX-XXXXX) always have a dash.
        var entered = (code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal);
        var isRecoveryCode = entered.Length == 0 || !entered.All(char.IsAsciiDigit);

        // A wrong code of either kind counts as a failed access, so it leads to lockout like a wrong password.
        var result = isRecoveryCode
            ? await RecoveryCodeSignInAsync(user, entered)
            : await signIn.TwoFactorAuthenticatorSignInAsync(entered, isPersistent: false, rememberClient: false);

        if (result.Succeeded)
        {
            if (isRecoveryCode)
                audit.RecoveryCodeUsed(user.Id);
            audit.LoginSucceeded(user.Id, ip);
            return SignInOutcome.Succeeded;
        }

        if (result.IsLockedOut)
            audit.AccountLockedOut(user.Id, ip);
        else
            audit.LoginFailed(user.Id, ip, isRecoveryCode ? "bad-recovery-code" : "bad-2fa-code");

        return isRecoveryCode ? SignInOutcome.InvalidRecoveryCode : SignInOutcome.InvalidAuthenticatorCode;
    }

    // SignInManager answers NotAllowed (email not confirmed) before it looks at the password, so the password is checked
    // here: the "confirm your email" hint goes only to someone who knows it, and a wrong one counts toward lockout as
    // anywhere else (docs/plan/RESEARCH.md R-067).
    private async Task<SignInResult> CheckUnconfirmedAsync(ApplicationUser user, string password)
    {
        if (await users.IsLockedOutAsync(user))
            return SignInResult.LockedOut;

        if (await users.CheckPasswordAsync(user, password))
            return SignInResult.NotAllowed;

        await users.AccessFailedAsync(user);
        return await users.IsLockedOutAsync(user) ? SignInResult.LockedOut : SignInResult.Failed;
    }

    // SignInManager redeems a recovery code without looking at lockout and doesn't count a wrong one: "codes are expected
    // to be random" (docs/plan/RESEARCH.md R-085). Both happen here, so a locked account can't get in with a code and a
    // wrong code counts like a wrong authenticator code. A redeemed code is gone (single use).
    private async Task<SignInResult> RecoveryCodeSignInAsync(ApplicationUser user, string code)
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
}
