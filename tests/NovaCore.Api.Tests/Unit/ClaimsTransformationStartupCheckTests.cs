using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NovaCore.AspNetCore.Authorization;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

/// <summary>
/// ASP.NET Core runs one <see cref="IClaimsTransformation"/>, the last one registered (docs/plan/RESEARCH.md R-122), and
/// the core's is what gives a caller the permissions <see cref="IPermissionResolver"/> resolves and drops the token's own.
/// A project's transformation registered after it would switch permission resolution off without an error (every
/// protected endpoint 403, the token's permission claims kept); one registered before it would never run. So the host
/// doesn't start with any other (PLAN F7b.7).
/// </summary>
public class ClaimsTransformationStartupCheckTests
{
    [Fact]
    public async Task A_transformation_registered_after_the_core_stops_the_host()
    {
        using var host = BuildProductionHost(services => services.AddScoped<IClaimsTransformation, ProjectTransformation>());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        Assert.Contains(nameof(IClaimsTransformation), error.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ProjectTransformation), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_transformation_registered_before_the_core_stops_the_host()
    {
        using var host = BuildProductionHost(
            services => services.AddScoped<IClaimsTransformation, ProjectTransformation>(), registerFirst: true);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        Assert.Contains(nameof(ProjectTransformation), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_host_starts_with_the_core_transformation_and_the_frameworks_no_op()
    {
        // AddAuthentication registers NoopClaimsTransformation unless there is one, as AddCoreWeb does before the core's.
        using var host = BuildProductionHost(services => services.AddAuthentication(), registerFirst: true);

        await host.StartAsync();
        await host.StopAsync();
    }

    private static IHost BuildProductionHost(Action<IServiceCollection> project, bool registerFirst = false)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });

        if (registerFirst)
            project(builder.Services);
        builder.Services.AddHybridCache();
        builder.Services.AddCoreAuthorization();
        builder.Services.AddSingleton<IPermissionResolver, NoPermissions>();
        if (!registerFirst)
            project(builder.Services);

        return builder.Build();
    }

    private sealed class ProjectTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) => Task.FromResult(principal);
    }

    private sealed class NoPermissions : IPermissionResolver
    {
        public Task<IReadOnlyCollection<string>> GetPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<string>>([]);
    }
}
