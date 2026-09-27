using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NovaCore.Identity.OpenIddict;

/// <summary>
/// When <c>Identity:Seed:SeedOnStartup</c> is true, syncs the configured clients into the store on startup: a missing
/// client is created, an existing one is updated to match the configuration. Clients that are no longer configured are
/// left alone.
/// </summary>
internal sealed partial class IdentityServerSeeder(
    IServiceProvider serviceProvider,
    IOptions<IdentityServerOptions> options,
    ILogger<IdentityServerSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Seed.SeedOnStartup)
            return;

        await using var scope = serviceProvider.CreateAsyncScope();
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

    private static OpenIddictApplicationDescriptor Describe(IdentityClientOptions client)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientSecret = string.IsNullOrEmpty(client.ClientSecret) ? null : client.ClientSecret,
            ClientType = string.IsNullOrEmpty(client.ClientSecret) ? ClientTypes.Public : ClientTypes.Confidential,
            DisplayName = client.DisplayName,
            // First-party clients, configured by the host: no consent screen.
            ConsentType = ConsentTypes.Implicit,
            Permissions =
            {
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
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        };

        descriptor.RedirectUris.UnionWith(client.RedirectUris);
        descriptor.PostLogoutRedirectUris.UnionWith(client.PostLogoutRedirectUris);
        return descriptor;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Identity seed: created client {ClientId}.")]
    private partial void LogClientCreated(string clientId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Identity seed: updated client {ClientId}.")]
    private partial void LogClientUpdated(string clientId);
}
