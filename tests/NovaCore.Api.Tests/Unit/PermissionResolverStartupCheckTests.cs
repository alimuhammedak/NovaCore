using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaCore.AspNetCore.Authorization;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// Permissions come from an <see cref="IPermissionResolver"/> the host registers (D-024 (7)). Without one the host must
/// not start, in every environment: DI validation would catch it only where ValidateOnBuild is on (Development by
/// default, R-029), and elsewhere every authenticated request would fail instead.
/// </summary>
public class PermissionResolverStartupCheckTests
{
    [Fact]
    public async Task Outside_development_the_host_refuses_to_start_without_a_permission_resolver()
    {
        using var host = BuildProductionHost(registerResolver: false);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        Assert.Contains(nameof(IPermissionResolver), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_host_starts_when_a_permission_resolver_is_registered()
    {
        using var host = BuildProductionHost(registerResolver: true);

        await host.StartAsync();
        await host.StopAsync();
    }

    private static IHost BuildProductionHost(bool registerResolver)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Services.AddCoreAuthorization();
        if (registerResolver)
            builder.Services.AddSingleton<IPermissionResolver, NoPermissions>();

        return builder.Build();
    }

    private sealed class NoPermissions : IPermissionResolver
    {
        public Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<string>>([]);
    }
}
