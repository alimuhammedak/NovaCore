using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Api.Tests.IdentityServer;
using NovaCore.AspNetCore.Identity;
using NovaCore.Identity.OpenIddict;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Infrastructure;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// The Api host's own wiring of the identity provider (Program.cs, PLAN F3b.3): with <c>Jwt:Authority</c> pointing at
/// the host itself, a user signs in through Authorization Code + PKCE and the modules' endpoints accept the token, with
/// the permissions resolved from the provider's store. The provider's behaviour is covered on its own host
/// (<c>IdentityServer/</c>); this checks that Program.cs puts the pieces together.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class IdentityServerWiringTests(IdentityServerWiringTests.ApiFactory factory)
    : IClassFixture<IdentityServerWiringTests.ApiFactory>
{
    private const string Password = "a long passphrase for tests";

    /// <summary>The Api host as its own authority, its JwtBearer backchannel on the in-memory server.</summary>
    public sealed class ApiFactory : NovaCoreApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) { ["Jwt:Authority"] = IdentityServerTestHost.Authority }));
            builder.ConfigureServices(services => services.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options => options.BackchannelHttpHandler = new ServerHandler(() => Server)));
        }

        private sealed class ServerHandler(Func<TestServer> server) : HttpMessageHandler
        {
            private HttpMessageInvoker? _invoker;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                (_invoker ??= new HttpMessageInvoker(server().CreateHandler())).SendAsync(request, cancellationToken);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    _invoker?.Dispose();
                base.Dispose(disposing);
            }
        }
    }

    [Fact]
    public async Task A_user_signed_in_at_the_host_reaches_the_modules_with_the_permissions_of_the_store()
    {
        await PrepareAsync("reader@novacore.test", CatalogPermissions.ProductsRead);
        using var flow = new OidcFlow(factory.Server);

        var tokens = await flow.SignInAsync("reader@novacore.test", Password);

        Assert.Equal(HttpStatusCode.OK, (await flow.CallApiAsync("/api/v1/products", tokens.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync("/api/v1/payments", tokens.AccessToken)).StatusCode);
    }

    // The schemas, the user-facing client and a user holding one module permission. Seeding is off in the test host.
    private async Task PrepareAsync(string email, string permission)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        await services.GetRequiredService<IdentityServerDbContext>().Database.EnsureCreatedAsync();
        await services.GetRequiredService<CatalogDbContext>().Database.EnsureCreatedAsync();

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
