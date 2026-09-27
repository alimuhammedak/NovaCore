using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>
/// Stops the host when no <see cref="IPermissionResolver"/> is registered, in every environment. The claims
/// transformation needs one for each authenticated request; DI validation would catch a missing one only where
/// ValidateOnBuild is on (Development by default), and elsewhere every authenticated request would fail instead.
/// </summary>
internal sealed class PermissionResolverStartupCheck(IServiceProviderIsService services) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        services.IsService(typeof(IPermissionResolver))
            ? Task.CompletedTask
            : throw new InvalidOperationException(
                $"No {nameof(IPermissionResolver)} is registered. Permissions are resolved on the server, so register "
                + "the identity integration's resolver (e.g. the Identity module's) or your own.");

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
