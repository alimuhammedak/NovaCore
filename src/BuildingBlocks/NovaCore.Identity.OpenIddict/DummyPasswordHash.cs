using Microsoft.AspNetCore.Identity;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// A password hash that belongs to no account. The sign-in checks the typed password against it when the email is
/// unknown or the account is locked, so either costs the same hash as a wrong password and the time of the answer doesn't
/// tell them apart (OWASP Authentication Cheat Sheet; <c>SignInManager</c> returns at once for an unknown user and before
/// the password for a locked one, docs/plan/RESEARCH.md R-087, R-138). The hash is made once, by the host's own hasher, so a host that replaces the hasher pays its cost too. The reset
/// codes get a dummy of their own hasher the same way (<see cref="CheckCode"/>).
/// </summary>
internal sealed class DummyPasswordHash
{
    private string? _hash;
    private string? _codeHash;

    // IPasswordHasher<TUser> is scoped (AddIdentityCore), so it comes with each call; the first call makes the hash.
    public void Check(IPasswordHasher<ApplicationUser> hasher, string password) => Verify(ref _hash, hasher, password);

    // The reset codes' hasher (PasswordResetCodes.HasherKey) has a cost of its own, so its dummy is a hash of its own: a
    // reset for an address without a code checks the typed code against it (D-091 (4)).
    public void CheckCode(IPasswordHasher<ApplicationUser> codeHasher, string code) => Verify(ref _codeHash, codeHasher, code);

    private static void Verify(ref string? cached, IPasswordHasher<ApplicationUser> hasher, string value)
    {
        var nobody = new ApplicationUser { CreatedAtUtc = default };
        var hash = LazyInitializer.EnsureInitialized(ref cached, () => hasher.HashPassword(nobody, Guid.NewGuid().ToString()));
        hasher.VerifyHashedPassword(nobody, hash, value);
    }
}
