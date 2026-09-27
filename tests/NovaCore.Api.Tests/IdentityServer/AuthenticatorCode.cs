using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Computes an authenticator app's code for a shared key, as the app would: TOTP (RFC 6238) over HMAC-SHA1 with 30-second
/// steps and six digits, the parameters ASP.NET Core Identity's authenticator provider verifies.
/// </summary>
internal static class AuthenticatorCode
{
    // RFC 6238 and Identity's verifier use HMAC-SHA1. The code only stands in for an authenticator app in a test, so it
    // needs no security guarantee: the case CA5350's documentation allows suppressing (docs/plan/RESEARCH.md R-063).
    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "TOTP (RFC 6238) is defined over HMAC-SHA1; test-only stand-in for an authenticator app.")]
    public static string For(string sharedKey)
    {
        var step = TimeProvider.System.GetUtcNow().ToUnixTimeSeconds() / 30;
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);

        var hash = HMACSHA1.HashData(FromBase32(sharedKey), counter);
        var offset = hash[^1] & 0x0F;
        var value = (BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & 0x7FFFFFFF) % 1_000_000;
        return value.ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <summary>A six-digit code that is not the current one.</summary>
    public static string WrongFor(string sharedKey) =>
        ((int.Parse(For(sharedKey), CultureInfo.InvariantCulture) + 500_000) % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);

    // RFC 4648 base32, the encoding of Identity's authenticator keys.
    private static byte[] FromBase32(string text)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;

        foreach (var c in text.TrimEnd('=').ToUpperInvariant())
        {
            buffer = (buffer << 5) | alphabet.IndexOf(c, StringComparison.Ordinal);
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)(buffer >> bits));
            }
        }

        return [.. bytes];
    }
}
