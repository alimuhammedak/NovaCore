using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace NovaCore.AspNetCore.Web;

/// <summary>
/// Bound from the <c>RateLimiting</c> section: the per-client fixed window every request shares, a client being an IPv4
/// address or an IPv6 /64. The opt-in <c>"strict"</c> policy allows a tenth of <see cref="PermitLimit"/> in the same
/// window.
/// </summary>
internal sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 100;

    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; set; } = 60;
}

[OptionsValidator]
internal sealed partial class RateLimitingOptionsValidator : IValidateOptions<RateLimitingOptions>;
