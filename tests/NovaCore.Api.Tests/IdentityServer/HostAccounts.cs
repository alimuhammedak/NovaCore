using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Identity;
using NovaCore.Identity.OpenIddict;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// What signing in through the Api host's own identity provider needs while seeding is off (the test hosts' default):
/// the user-facing client <see cref="OidcFlow"/> plays, and a user holding one module permission.
/// </summary>
internal static class HostAccounts
{
    public const string Password = "a long passphrase for tests";

    /// <summary>Adds the client (once) and a confirmed user with <paramref name="permission"/>, in the host's store.</summary>
    public static async Task AddClientAndUserAsync(IServiceProvider services, string email, string permission)
    {
        var applications = services.GetRequiredService<IOpenIddictApplicationManager>();
        if (await applications.FindByClientIdAsync(IdentityServerTestHost.ClientId) is null)
        {
            var client = new OpenIddictApplicationDescriptor
            {
                ClientId = IdentityServerTestHost.ClientId,
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                Permissions =
                {
                    Permissions.Endpoints.Authorization, Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode, Permissions.GrantTypes.RefreshToken, Permissions.ResponseTypes.Code
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange }
            };
            client.RedirectUris.Add(new Uri(IdentityServerTestHost.RedirectUri));
            await applications.CreateAsync(client);
        }

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, CreatedAtUtc = TimeProvider.System.GetUtcNow() };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        await users.AddClaimAsync(user, new Claim(NovaClaims.Permission, permission));
    }
}
