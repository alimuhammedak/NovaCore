using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// A limit per recipient address on the anonymous endpoints that email one (registration, resending the confirmation,
/// the reset link and the reset code), on top of the rate limiter's per-client partitions (S-6, docs/plan/RESEARCH.md
/// R-157). A caller who changes client addresses gets past those, but not past this one: one mailbox can't be flooded, its
/// owner's outstanding reset code can't be replaced over and over, and one address can't fill <see cref="EmailQueue"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>The rate limiter middleware can't key on the address: a policy's partitioner runs before the endpoint has read
/// the body. So the handlers ask this limit first, with a <see cref="PartitionedRateLimiter{TResource}"/> from
/// <c>System.Threading.RateLimiting</c>, the library the middleware uses.</item>
/// <item>It counts the address the request names, before anything looks the account up, so an address without an account
/// is limited exactly like one with an account: the 429 reveals no account, and a refused request costs no password
/// hash, issues no code and queues no email.</item>
/// <item>The key is Identity's normalized email (<c>UserManager.NormalizeEmail</c>), the form the store looks accounts up
/// by, so a change of case is the same address.</item>
/// <item>The counts live in this process's memory, like the rate limiter's: each instance of a scaled-out host counts
/// its own. A partition goes once its window has passed unused.</item>
/// </list>
/// </remarks>
internal sealed class EmailRecipientLimit : IDisposable
{
    /// <summary>The 429 every refused address gets, whether or not it has an account.</summary>
    public static ProblemHttpResult TooManyEmails { get; } = TypedResults.Problem(
        detail: "Too many emails were asked for this address. Try again later.",
        statusCode: StatusCodes.Status429TooManyRequests,
        title: "Too many requests",
        type: $"https://httpstatuses.io/{StatusCodes.Status429TooManyRequests}",
        extensions: new Dictionary<string, object?>(StringComparer.Ordinal) { ["code"] = "account.too_many_emails" });

    private readonly PartitionedRateLimiter<string> _limiter;

    public EmailRecipientLimit(IOptions<IdentityServerOptions> identity)
    {
        var limit = identity.Value.EmailLimit;
        var permitLimit = limit.PermitLimit;
        var window = TimeSpan.FromSeconds(limit.WindowSeconds);

        _limiter = PartitionedRateLimiter.Create<string, string>(
            address => RateLimitPartition.GetFixedWindowLimiter(address, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0
            }),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Counts a request for <paramref name="normalizedEmail"/> and answers whether it may go on. Call it before the
    /// account is looked up.
    /// </summary>
    public bool TryAcquire(string? normalizedEmail)
    {
        using var lease = _limiter.AttemptAcquire(normalizedEmail ?? string.Empty);
        return lease.IsAcquired;
    }

    public void Dispose() => _limiter.Dispose();
}
