using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Identity's Entity Framework user store, keeping two-factor recovery codes as password hashes (ASVS 6.5.2). Identity
/// stores them as plain text in a user token, so anyone who reads the table can sign in past the second factor.
/// </summary>
/// <remarks>
/// A code has about 47 bits (10 characters from 26), under the 112 bits below which NIST SP 800-63B wants a salted,
/// approved key derivation function. The hasher is Identity's <see cref="PasswordHasher{TUser}"/> with its defaults
/// (PBKDF2-HMAC-SHA512, 100,000 iterations), not the host's 600,000: a sign-in tries up to ten hashes. The hashes live
/// under a token name of their own, so a plain-text code stored by Identity never matches (DECISIONS D-053 (3)).
/// It also reports a token lost to a concurrent request as a concurrency failure, like the base class reports a user
/// changed by one (<see cref="UpdateAsync"/>).
/// </remarks>
internal sealed class HashedRecoveryCodesUserStore(IdentityServerDbContext context, IdentityErrorDescriber? describer = null)
    : UserStore<ApplicationUser, ApplicationRole, IdentityServerDbContext, Guid>(context, describer)
{
    private const string LoginProvider = "[NovaCore]";
    private const string TokenName = "RecoveryCodeHashes";

    // Where Identity's own store keeps the codes as plain text (UserStoreBase, private there). Versions before the hashing
    // wrote them; new codes take their place, so the plain-text ones don't stay at rest.
    private const string PlainTextLoginProvider = "[AspNetUserStore]";
    private const string PlainTextTokenName = "RecoveryCodes";

    private readonly PasswordHasher<ApplicationUser> _hasher = new();

    /// <summary>
    /// Saves the user, answering <see cref="IdentityErrorDescriber.ConcurrencyFailure"/> when a token it adds was added by
    /// another request in the meantime. Two requests verifying a user's first authenticator code (or replacing their first
    /// recovery codes) both add the token: the loser's save fails on the token's key, not on the user's concurrency stamp,
    /// so without this it would surface as a 500 instead of a refused code (DECISIONS D-058).
    /// </summary>
    public override async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.UpdateAsync(user, cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            if (!await ForgetLostTokensAsync(exception, cancellationToken))
                throw;

            return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
        }
    }

    public override async Task ReplaceCodesAsync(ApplicationUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(recoveryCodes);

        await RemoveTokenAsync(user, PlainTextLoginProvider, PlainTextTokenName, cancellationToken);
        await SetTokenAsync(user, LoginProvider, TokenName,
            string.Join(';', recoveryCodes.Select(code => _hasher.HashPassword(user, code))), cancellationToken);
    }

    public override async Task<bool> RedeemCodeAsync(ApplicationUser user, string code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(code);

        var hashes = await HashesAsync(user, cancellationToken);
        var match = hashes.FindIndex(hash => _hasher.VerifyHashedPassword(user, hash, code) != PasswordVerificationResult.Failed);
        if (match < 0)
            return false;

        // UserManager saves the user with its concurrency stamp next, so two requests racing with one code redeem it once.
        hashes.RemoveAt(match);
        await SetTokenAsync(user, LoginProvider, TokenName, string.Join(';', hashes), cancellationToken);
        return true;
    }

    public override async Task<int> CountCodesAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return (await HashesAsync(user, cancellationToken)).Count;
    }

    // The added tokens of the failed save that the database now holds: another request added them. They are detached, so
    // a later save of this context (a failed attempt counted toward lockout) doesn't add them again.
    private async Task<bool> ForgetLostTokensAsync(DbUpdateException exception, CancellationToken cancellationToken)
    {
        var lost = new List<EntityEntry>();
        foreach (var entry in exception.Entries)
        {
            if (entry is { State: EntityState.Added, Entity: IdentityUserToken<Guid> token }
                && await Context.Set<IdentityUserToken<Guid>>().AsNoTracking().AnyAsync(stored =>
                    stored.UserId == token.UserId && stored.LoginProvider == token.LoginProvider && stored.Name == token.Name,
                    cancellationToken))
                lost.Add(entry);
        }

        foreach (var entry in lost)
            entry.State = EntityState.Detached;
        return lost.Count > 0;
    }

    private async Task<List<string>> HashesAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        [.. (await GetTokenAsync(user, LoginProvider, TokenName, cancellationToken) ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries)];
}
