using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace MyService.Api;

/// <summary>
/// Bound from the <c>HealthChecks</c> section: the management port the orchestrator's probes (<c>/health</c>,
/// <c>/alive</c>) are answered on outside Development. Empty answers no probes there. The host must listen on the port
/// too (e.g. <c>ASPNETCORE_HTTP_PORTS=8080;8081</c>); a value that isn't a port stops the host at startup.
/// </summary>
internal sealed class HealthCheckProbeOptions
{
    public const string SectionName = "HealthChecks";

    [Range(1, 65535)]
    public int? ProbePort { get; set; }
}

[OptionsValidator]
internal sealed partial class HealthCheckProbeOptionsValidator : IValidateOptions<HealthCheckProbeOptions>;
