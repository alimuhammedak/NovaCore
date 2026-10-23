using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Identity;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// Permission-based authorization end to end: a permission granted on the server opens its endpoint and nothing else,
/// an authenticated caller without the permission gets 403 (anonymous callers get 401, <see cref="AuthGateTests"/>), and
/// neither a permission claim nor the Administrator role in the token itself grants anything (PLAN F3a.2, F4c.2). Written against the dynamic policy provider before F2c.2 replaced it with
/// <c>RequirePermissionAttribute</c> (IAuthorizationRequirementData).
/// </summary>
public sealed class PermissionAuthorizationTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>, IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_granted_permission_opens_its_endpoint()
    {
        using var client = factory.CreateClientWithPermissions(CatalogPermissions.ProductsRead);

        var response = await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_granted_permission_does_not_open_another_endpoint()
    {
        using var client = factory.CreateClientWithPermissions(CatalogPermissions.ProductsRead);

        var response = await client.PostAsJsonAsync("/api/v1/products", new { name = "Widget", price = 9.99m, stock = 5 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_administrator_role_in_the_token_grants_nothing()
    {
        // A token's roles say who the caller was when it was issued; a removed administrator keeps the claim until the
        // token expires. Permissions, an administrator's included, come from the server (F4c.2).
        using var client = ClientWith(new Claim(NovaClaims.Role, "Administrator"));

        var response = await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_authenticated_caller_without_the_permission_is_forbidden()
    {
        using var client = factory.CreateClientWithPermissions();

        var response = await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_permission_claim_in_the_token_grants_nothing()
    {
        using var client = ClientWith(new Claim(NovaClaims.Permission, CatalogPermissions.ProductsRead));

        var response = await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient ClientWith(params Claim[] claims) =>
        factory.CreateClientWithClaims([new Claim(NovaClaims.Subject, Guid.NewGuid().ToString()), .. claims]);
}
