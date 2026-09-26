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
/// Permission-based authorization end to end, with tokens that carry exactly the claims under test: a permission claim
/// grants its endpoint and nothing else, the Administrator role grants every endpoint, and an authenticated caller
/// without the permission gets 403 (anonymous callers get 401, <see cref="AuthGateTests"/>). Written against the dynamic
/// policy provider before F2c.2 replaced it with <c>RequirePermissionAttribute</c> (IAuthorizationRequirementData).
/// </summary>
public sealed class PermissionAuthorizationTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>, IAsyncLifetime
{
    private readonly NovaCoreApiFactory _factory = factory;

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_permission_claim_grants_its_endpoint()
    {
        using var client = ClientWith(new Claim(NovaClaims.Permission, CatalogPermissions.ProductsRead));

        var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task A_permission_claim_does_not_grant_another_endpoint()
    {
        using var client = ClientWith(new Claim(NovaClaims.Permission, CatalogPermissions.ProductsRead));

        var response = await client.PostAsJsonAsync("/api/v1/products", new { name = "Widget", price = 9.99m, stock = 5 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_administrator_role_grants_every_endpoint()
    {
        using var client = ClientWith(new Claim(NovaClaims.Role, HttpContextCurrentUser.SuperAdminRole));

        var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task An_authenticated_caller_without_the_permission_is_forbidden()
    {
        using var client = ClientWith();

        var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient ClientWith(params Claim[] claims) =>
        _factory.CreateClientWithClaims([new Claim(NovaClaims.Subject, Guid.NewGuid().ToString()), .. claims]);
}
