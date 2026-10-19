using System.Net;
using System.Net.Sockets;

namespace NovaCore.AspNetCore.Web;

/// <summary>
/// Bound from the <c>ForwardedHeaders</c> section: the reverse proxies (<see cref="KnownProxies"/>, IP addresses) and
/// networks (<see cref="KnownIPNetworks"/>, CIDR) whose <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> the host
/// applies. Empty: no caller's are applied, the framework default. Configures ASP.NET Core's <c>ForwardedHeadersOptions</c>.
/// </summary>
internal sealed class TrustedProxiesOptions
{
    public const string SectionName = "ForwardedHeaders";

    public string[] KnownProxies { get; set; } = [];

    public string[] KnownIPNetworks { get; set; } = [];

    public bool IsEmpty => KnownProxies.Length == 0 && KnownIPNetworks.Length == 0;

    /// <summary>
    /// An IP address as written. <see cref="IPAddress.TryParse(string, out IPAddress)"/> also takes shorthand IPv4 such as
    /// <c>10.0.0</c> (read as <c>10.0.0.0</c>), which would trust an address nobody meant, so IPv4 must have four parts.
    /// </summary>
    public static bool IsAddress(string value) =>
        IPAddress.TryParse(value, out var address)
        && (address.AddressFamily != AddressFamily.InterNetwork || address.ToString() == value);

    /// <summary>A network in CIDR notation, e.g. <c>10.0.0.0/8</c>, with no host bits set.</summary>
    public static bool IsNetwork(string value) => System.Net.IPNetwork.TryParse(value, out _);
}
