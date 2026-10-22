using System.Globalization;
using Microsoft.AspNetCore.Identity;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// Identity's authenticator-app provider with every code accepted once (ASVS 6.5.1): the sign-in page, enabling and
/// disabling two-factor authentication all verify through it. Identity accepts a code for two 30-second steps either
/// side of now and records nothing, so a code seen over a shoulder or in a proxy log works again for up to two minutes
/// (docs/plan/RESEARCH.md R-099).
/// </summary>
/// <remarks>
/// Identity computes the codes; this class only remembers the accepted ones, in a user token. It doesn't compute TOTP
/// itself: Identity's implementation is internal, and its HMAC-SHA1 is no case CA5350 allows (DECISIONS D-053 (2)).
/// </remarks>
internal sealed class SingleUseAuthenticatorTokenProvider(TimeProvider timeProvider) : AuthenticatorTokenProvider<ApplicationUser>
{
    private const string LoginProvider = "[NovaCore]";
    private const string TokenName = "UsedAuthenticatorCodes";

    // A code accepted now belongs to a step at most two ahead, which is accepted until two steps after it: the codes
    // accepted in the last five steps are the ones that could still be replayed.
    private const long ReplayWindowSteps = 4;

    public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        if (!await base.ValidateAsync(purpose, token, manager, user))
            return false;

        // The base class compares the codes as numbers with this parse, so "012345" and "12345" are one code.
        var code = int.Parse(token, CultureInfo.CurrentCulture);
        var step = timeProvider.GetUtcNow().ToUnixTimeSeconds() / 30;

        var recent = Parse(await manager.GetAuthenticationTokenAsync(user, LoginProvider, TokenName))
            .Where(used => used.Step >= step - ReplayWindowSteps)
            .ToList();
        if (recent.Exists(used => used.Code == code))
            return false;

        recent.Add((step, code));

        // The user is saved with its concurrency stamp: of two requests racing with one code, the second save fails.
        var saved = await manager.SetAuthenticationTokenAsync(user, LoginProvider, TokenName,
            string.Join(';', recent.Select(used => string.Create(CultureInfo.InvariantCulture, $"{used.Step}:{used.Code}"))));
        return saved.Succeeded;
    }

    private static IEnumerable<(long Step, int Code)> Parse(string? value)
    {
        foreach (var entry in (value ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split(':');
            if (parts.Length == 2
                && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var step)
                && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var code))
                yield return (step, code);
        }
    }
}
