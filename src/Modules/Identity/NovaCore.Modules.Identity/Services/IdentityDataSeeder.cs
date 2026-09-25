using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NovaCore.Core.Web.Authorization;
using NovaCore.Core.Web.Identity;
using NovaCore.Modules.Identity.Authorization;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Options;

namespace NovaCore.Modules.Identity.Services;

/// <summary>
/// Seeds the two baseline roles and their permission claims (collected from every module's
/// <see cref="IPermissionDefinitionProvider"/>), plus an optional initial admin. Idempotent.
/// </summary>
public sealed partial class IdentityDataSeeder(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IEnumerable<IPermissionDefinitionProvider> permissionProviders,
    IOptions<IdentitySeedOptions> seedOptions,
    ILogger<IdentityDataSeeder> logger)
{
    public const string AdministratorRole = HttpContextCurrentUser.SuperAdminRole; // "Administrator"
    public const string UserRole = "User";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var permissions = permissionProviders
            .SelectMany(p => p.GetPermissions())
            .DistinctBy(p => p.Name)
            .ToList();

        // Administrator holds every permission (and is also a super-admin that bypasses checks).
        await EnsureRoleAsync(AdministratorRole, "Full administrative access", permissions.Select(p => p.Name));

        // User gets least-privilege: read-only permissions for business modules, but NOT the
        // Identity-admin reads (a normal user shouldn't enumerate accounts/roles).
        await EnsureRoleAsync(UserRole, "Standard authenticated user", permissions
            .Where(p => p.Name.EndsWith(":read", StringComparison.Ordinal)
                        && !string.Equals(p.Group, IdentityPermissions.Group, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name));

        await EnsureAdminUserAsync();
    }

    private async Task EnsureRoleAsync(string roleName, string description, IEnumerable<string> permissionNames)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new ApplicationRole(roleName) { Description = description };
            var created = await roleManager.CreateAsync(role);
            if (!created.Succeeded)
            {
                LogRoleNotCreated(roleName, string.Join(", ", created.Errors.Select(e => e.Description)));
                return;
            }
        }

        var existing = (await roleManager.GetClaimsAsync(role))
            .Where(c => c.Type == NovaClaims.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var permission in permissionNames)
            if (existing.Add(permission))
                await roleManager.AddClaimAsync(role, new Claim(NovaClaims.Permission, permission));
    }

    private async Task EnsureAdminUserAsync()
    {
        var options = seedOptions.Value;

        if (string.IsNullOrWhiteSpace(options.AdminEmail) || string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            LogAdminCredentialsMissing();
            return;
        }

        if (await userManager.FindByEmailAsync(options.AdminEmail) is not null)
            return;

        var admin = new ApplicationUser
        {
            UserName = options.AdminEmail,
            Email = options.AdminEmail,
            EmailConfirmed = true,
            DisplayName = "Administrator"
        };

        var result = await userManager.CreateAsync(admin, options.AdminPassword);
        if (!result.Succeeded)
        {
            LogAdminNotCreated(string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, AdministratorRole);
        LogAdminCreated(options.AdminEmail);
    }

    [LoggerMessage(EventId = 21, Level = LogLevel.Error, Message = "Identity seed: could not create role {Role}: {Errors}")]
    private partial void LogRoleNotCreated(string role, string errors);

    [LoggerMessage(EventId = 22, Level = LogLevel.Warning, Message = "Identity seed: Identity:Seed:AdminEmail/AdminPassword not set — skipping admin user.")]
    private partial void LogAdminCredentialsMissing();

    [LoggerMessage(EventId = 23, Level = LogLevel.Error, Message = "Identity seed: could not create admin user: {Errors}")]
    private partial void LogAdminNotCreated(string errors);

    [LoggerMessage(EventId = 24, Level = LogLevel.Information, Message = "Identity seed: created admin user {Email}.")]
    private partial void LogAdminCreated(string email);
}

/// <summary>Runs <see cref="IdentityDataSeeder"/> on startup when <c>Identity:Seed:SeedOnStartup</c> is true.</summary>
public sealed partial class IdentitySeederHostedService(
    IServiceProvider serviceProvider,
    IOptions<IdentitySeedOptions> options,
    ILogger<IdentitySeederHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.SeedOnStartup)
            return;

        LogSeeding();
        await using var scope = serviceProvider.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 25, Level = LogLevel.Information, Message = "Identity seed: running startup seeding...")]
    private partial void LogSeeding();
}
