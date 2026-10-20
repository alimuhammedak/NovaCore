using Microsoft.AspNetCore.Identity;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// A password hash that belongs to no account. The sign-in page checks the typed password against it when the email is
/// unknown, so an unknown address costs the same hash as a known one and the time of the answer doesn't tell them apart
/// (OWASP Authentication Cheat Sheet; <c>SignInManager</c> returns at once for an unknown user, docs/plan/RESEARCH.md
/// R-087). The hash is made once, by the host's own hasher, so a host that replaces the hasher pays its cost too.
/// </summary>
internal sealed class DummyPasswordHash
{
    private string? _hash;

    // IPasswordHasher<TUser> is scoped (AddIdentityCore), so it comes with each call; the first call makes the hash.
    public void Check(IPasswordHasher<ApplicationUser> hasher, string password)
    {
        var nobody = new ApplicationUser { CreatedAtUtc = default };
        var hash = LazyInitializer.EnsureInitialized(ref _hash, () => hasher.HashPassword(nobody, Guid.NewGuid().ToString()));
        hasher.VerifyHashedPassword(nobody, hash, password);
    }
}
