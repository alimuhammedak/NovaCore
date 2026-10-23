using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// When <c>Identity:Seed:SeedOnStartup</c> is true, seeds the store on startup. Idempotent:
/// <list type="bullet">
///   <item>the Administrator role gets every permission the modules declare, the User role the ones
///   <c>Identity:Seed:UserPermissions</c> lists (least privilege: a list the host chose, not a naming rule);</item>
///   <item>the initial administrator is created when <c>Identity:Seed:AdminEmail</c> and <c>AdminPassword</c> are set
///   (the role counts once they turn two-factor authentication on);</item>
///   <item>the configured clients are created or updated to match; clients no longer configured are left alone.</item>
/// </list>
/// </summary>
internal sealed partial class IdentityServerSeeder(
    IServiceProvider serviceProvider,
    IEnumerable<IPermissionDefinitionProvider> permissionProviders,
    IOptions<IdentityServerOptions> options,
    TimeProvider timeProvider,
    ILogger<IdentityServerSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Seed.SeedOnStartup)
            return;

        await using var scope = serviceProvider.CreateAsyncScope();

        await SeedRolesAsync(scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>());
        await SeedAdministratorAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());

        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var client in options.Value.Clients)
        {
            var descriptor = Describe(client);
            var application = await applications.FindByClientIdAsync(client.ClientId, cancellationToken);

            if (application is null)
            {
                await applications.CreateAsync(descriptor, cancellationToken);
                LogClientCreated(client.ClientId);
            }
            else
            {
                await applications.UpdateAsync(application, descriptor, cancellationToken);
                LogClientUpdated(client.ClientId);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedRolesAsync(RoleManager<ApplicationRole> roles)
    {
        var permissions = permissionProviders
            .SelectMany(provider => provider.GetPermissions())
            .DistinctBy(permission => permission.Name, StringComparer.Ordinal)
            .ToList();

        await EnsureRoleAsync(roles, IdentityRoles.Administrator, "Full administrative access",
            permissions.Select(permission => permission.Name));

        await EnsureRoleAsync(roles, IdentityRoles.User, "Standard authenticated user", options.Value.Seed.UserPermissions);
    }

    // Adds missing permissions to the role; never removes one an administrator granted.
    private async Task EnsureRoleAsync(RoleManager<ApplicationRole> roles, string name, string description, IEnumerable<string> permissions)
    {
        var role = await roles.FindByNameAsync(name);
        if (role is null)
        {
            role = new ApplicationRole(name) { Description = description };
            var created = await roles.CreateAsync(role);
            if (!created.Succeeded)
            {
                LogRoleNotCreated(name, string.Join(", ", created.Errors.Select(error => error.Description)));
                return;
            }
        }

        var existing = (await roles.GetClaimsAsync(role))
            .Where(claim => string.Equals(claim.Type, NovaClaims.Permission, StringComparison.Ordinal))
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var permission in permissions)
            if (existing.Add(permission))
                await roles.AddClaimAsync(role, new Claim(NovaClaims.Permission, permission));
    }

    private async Task SeedAdministratorAsync(UserManager<ApplicationUser> users)
    {
        var seed = options.Value.Seed;
        if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            LogAdministratorSkipped();
            return;
        }

        if (await users.FindByEmailAsync(seed.AdminEmail) is not null)
            return;

        var administrator = new ApplicationUser
        {
            UserName = seed.AdminEmail,
            Email = seed.AdminEmail,
            EmailConfirmed = true,
            DisplayName = "Administrator",
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        var created = await users.CreateAsync(administrator, seed.AdminPassword);
        if (!created.Succeeded)
        {
            LogAdministratorNotCreated(string.Join(", ", created.Errors.Select(error => error.Description)));
            return;
        }

        await users.AddToRoleAsync(administrator, IdentityRoles.Administrator);
        LogAdministratorCreated(seed.AdminEmail);
    }

    private static OpenIddictApplicationDescriptor Describe(IdentityClientOptions client)
    {
        var confidential = !string.IsNullOrEmpty(client.ClientSecret);
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientSecret = confidential ? client.ClientSecret : null,
            ClientType = confidential ? ClientTypes.Confidential : ClientTypes.Public,
            DisplayName = client.DisplayName,
            // First-party clients, configured by the host: no consent screen.
            ConsentType = ConsentTypes.Implicit
        };

        if (client.SignsInUsers)
        {
            descriptor.Permissions.UnionWith(
            [
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.Revocation,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Email,
                Permissions.Scopes.Roles
            ]);
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
            descriptor.RedirectUris.UnionWith(client.RedirectUris);
            descriptor.PostLogoutRedirectUris.UnionWith(client.PostLogoutRedirectUris);
        }
        else
        {
            descriptor.Permissions.UnionWith([Permissions.Endpoints.Token, Permissions.GrantTypes.ClientCredentials]);
        }

        return descriptor;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Identity seed: created client {ClientId}.")]
    private partial void LogClientCreated(string clientId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Identity seed: updated client {ClientId}.")]
    private partial void LogClientUpdated(string clientId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Identity seed: could not create role {Role}: {Errors}")]
    private partial void LogRoleNotCreated(string role, string errors);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Identity seed: Identity:Seed:AdminEmail/AdminPassword not set, so no administrator is created.")]
    private partial void LogAdministratorSkipped();

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Identity seed: could not create the administrator: {Errors}")]
    private partial void LogAdministratorNotCreated(string errors);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Identity seed: created the administrator {Email}.")]
    private partial void LogAdministratorCreated(string email);
}
