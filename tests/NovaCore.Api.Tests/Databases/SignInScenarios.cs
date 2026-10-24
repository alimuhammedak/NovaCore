using System.Net;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Api.Tests.IdentityServer;
using NovaCore.Modules.Catalog.Authorization;
using Xunit;

namespace NovaCore.Api.Tests.Databases;

/// <summary>
/// The identity provider add-on on a real database, the same on every provider: its store migrated by the host, a user
/// signs in at the host through Authorization Code + PKCE (the store writes the user's session, the authorization and the
/// tokens) and reaches the modules with the permissions of the store. The derived classes run in the
/// <see cref="IdentityServerTestGroup"/> collection, apart from the parallel tests.
/// </summary>
public abstract class SignInScenarios<TFactory>(TFactory factory) : IClassFixture<TFactory>
    where TFactory : DatabaseApiFactory
{
    protected TFactory Factory { get; } = factory;

    [Fact]
    public async Task A_user_signed_in_at_the_host_reaches_the_modules_with_the_permissions_of_the_store()
    {
        Factory.Database.SkipIfUnavailable();
        await using (var scope = Factory.Services.CreateAsyncScope())
            await HostAccounts.AddClientAndUserAsync(scope.ServiceProvider, "reader@novacore.test", CatalogPermissions.ProductsRead);
        using var flow = new OidcFlow(Factory.Server);

        var tokens = await flow.SignInAsync("reader@novacore.test", HostAccounts.Password);

        Assert.Equal(HttpStatusCode.OK, (await flow.CallApiAsync("/api/v1/products", tokens.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await flow.CallApiAsync("/api/v1/payments", tokens.AccessToken)).StatusCode);
    }
}
