using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NovaCore.AspNetCore.Authorization;

/// <summary>
/// Stops the host when an <see cref="IClaimsTransformation"/> other than the core's is registered. ASP.NET Core runs one,
/// the last registered (docs/plan/RESEARCH.md R-122): one registered after the core's would switch permission resolution
/// off without an error (every protected endpoint 403, the token's own permission claims kept), one registered before it
/// would never run. The framework's own <see cref="NoopClaimsTransformation"/>, which <c>AddAuthentication</c> registers
/// unless there is one, doesn't count.
/// </summary>
internal sealed class ClaimsTransformationStartupCheck(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var others = scope.ServiceProvider.GetServices<IClaimsTransformation>()
            .Where(transformation => transformation is not (PermissionClaimsTransformation or NoopClaimsTransformation))
            .Select(transformation => transformation.GetType().FullName)
            .ToList();
        var effective = scope.ServiceProvider.GetRequiredService<IClaimsTransformation>();

        if (others.Count > 0 || effective is not PermissionClaimsTransformation)
            throw new InvalidOperationException(
                $"Another {nameof(IClaimsTransformation)} is registered "
                + $"({string.Join(", ", others.DefaultIfEmpty(effective.GetType().FullName))}). ASP.NET Core runs only "
                + "one, and NovaCore's resolves the caller's permissions through IPermissionResolver: add what yours adds "
                + "through an IPermissionResolver instead.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
