using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.EntityFrameworkCore.Persistence;
using NovaCore.Identity.OpenIddict;
using NovaCore.MultiTenancy;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The identity provider's permissions and a tenant membership's together (PLAN F7b.4): the core asks every resolver and
/// the caller holds what any of them grants, while the Administrator role still counts only with two-factor
/// authentication on (DECISIONS D-053 (5)), whatever the membership grants. The core's claims transformation runs here
/// for a request that names the tenant in its route, as it does before authorization.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class TenantPermissionTests(TenantPermissionTests.Fixture fixture) : IClassFixture<TenantPermissionTests.Fixture>
{
    /// <summary>The tenant's role, which grants <see cref="IdentityServerTestHost.ProbePermission"/> there.</summary>
    private const string Member = "Member";

    private readonly IdentityServerTestHost _host = fixture.Host;

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    /// <summary>The identity provider and the multi-tenancy add-on on one host, the tenancy store next to the identity store.</summary>
    public sealed class Fixture : IdentityServerFixture
    {
        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();

            await using var scope = Host.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<MultiTenancyDbContext>().GetService<IRelationalDatabaseCreator>()
                .CreateTablesAsync();
        }

        protected override void Configure(WebApplicationBuilder builder)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [$"MultiTenancy:Roles:{Member}:0"] = IdentityServerTestHost.ProbePermission
            });
            builder.Services.AddPermissionDefinitions<AdminTests.ProbePermissions>();
            builder.AddCoreMultiTenancy();
            builder.Services.AddModuleDbContext<MultiTenancyDbContext>(MultiTenancyDbContext.Schema, (_, _) => { });
        }
    }

    [Fact]
    public async Task An_administrator_without_two_factor_authentication_holds_only_what_the_membership_grants()
    {
        var user = await AdministratorAsync("tenant-admin-no-2fa@novacore.test", twoFactor: false);
        var tenant = await TenantWithMemberAsync(user);

        Assert.Equal([IdentityServerTestHost.ProbePermission], await PermissionsAsync(user, tenant));
        Assert.Empty(await PermissionsAsync(user, tenant: null));
    }

    [Fact]
    public async Task An_administrator_with_two_factor_authentication_holds_every_declared_permission_in_a_tenant_too()
    {
        var user = await AdministratorAsync("tenant-admin-2fa@novacore.test", twoFactor: true);
        var tenant = await TenantWithMemberAsync(user);

        await using var scope = _host.Services.CreateAsyncScope();
        var declared = scope.ServiceProvider.GetServices<IPermissionDefinitionProvider>()
            .SelectMany(provider => provider.GetPermissions())
            .Select(permission => permission.Name)
            .Order(StringComparer.Ordinal);

        Assert.Equal(declared, await PermissionsAsync(user, tenant), StringComparer.Ordinal);
    }

    [Fact]
    public async Task A_users_own_grants_and_the_memberships_add_up_in_the_tenant()
    {
        var user = await _host.CreateUserAsync("tenant-member@novacore.test", "a long passphrase for tests", "probe:audit:read");
        var tenant = await TenantWithMemberAsync(user);

        Assert.Equal(["probe:audit:read", IdentityServerTestHost.ProbePermission], await PermissionsAsync(user, tenant));
        Assert.Equal(["probe:audit:read"], await PermissionsAsync(user, tenant: null));
    }

    private async Task<ApplicationUser> AdministratorAsync(string email, bool twoFactor)
    {
        var user = await _host.CreateUserAsync(email, "a long passphrase for tests");
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var stored = (await users.FindByIdAsync(user.Id.ToString()))!;
        Assert.True((await users.AddToRoleAsync(stored, "Administrator")).Succeeded);
        Assert.True((await users.SetTwoFactorEnabledAsync(stored, twoFactor)).Succeeded);
        return stored;
    }

    private async Task<Guid> TenantWithMemberAsync(ApplicationUser user)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var directory = scope.ServiceProvider.GetRequiredService<TenantDirectory>();
        var tenant = await directory.CreateAsync("Tenant", Cancellation);
        Assert.True((await directory.AddMemberAsync(tenant, user.Id.ToString(), [Member], Cancellation)).IsSuccess);
        return tenant;
    }

    // The core's transformation for a request whose route names the tenant (or none), as authentication runs it.
    private async Task<string[]> PermissionsAsync(ApplicationUser user, Guid? tenant)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var request = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        if (tenant is { } id)
            request.Request.RouteValues[MultiTenancyHostingExtensions.TenantRouteValue] = id.ToString();

        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = request;
        try
        {
            var caller = new ClaimsPrincipal(new ClaimsIdentity([new Claim(NovaClaims.Subject, user.Id.ToString())], "test"));
            var transformed = await scope.ServiceProvider.GetRequiredService<IClaimsTransformation>().TransformAsync(caller);
            return [.. transformed.FindAll(NovaClaims.Permission).Select(claim => claim.Value).Order(StringComparer.Ordinal)];
        }
        finally
        {
            accessor.HttpContext = null;
        }
    }
}
