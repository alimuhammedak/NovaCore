using System.Net;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Catalog.Infrastructure;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// Permissions are resolved on the server and cached per subject and security stamp for <c>Cache:PermissionExpiration</c>
/// (PLAN F3a.2, D-024): a change reaches a signed-in caller once the cached entry expires, not before, and a token with
/// a new security stamp skips the cache. The clock is fake, so "expires" is a clock advance, not a wait. The tests here
/// use the test issuer's tokens, which read the real clock, so advancing the shared fake clock cannot invalidate them;
/// the Identity-issued variant is <see cref="RoleChangeTests"/>.
/// </summary>
public sealed class PermissionResolutionTests(FakeClockApiFactory factory)
    : IClassFixture<FakeClockApiFactory>, IAsyncLifetime
{
    // Cache:PermissionExpiration's default; the test host leaves it unset.
    private static readonly TimeSpan PermissionExpiration = TimeSpan.FromMinutes(5);

    public async ValueTask InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_revoked_permission_is_honoured_until_the_cached_entry_expires()
    {
        var subject = Guid.NewGuid().ToString();
        factory.GrantPermissions(subject, CatalogPermissions.ProductsRead);
        using var client = factory.CreateClientWithClaims(new Claim(NovaClaims.Subject, subject));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/products")).StatusCode);

        factory.GrantPermissions(subject);
        factory.Clock.Advance(PermissionExpiration - TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/products")).StatusCode);

        factory.Clock.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/products")).StatusCode);
    }

    [Fact]
    public async Task A_new_security_stamp_skips_the_cached_permissions()
    {
        var subject = Guid.NewGuid().ToString();
        factory.GrantPermissions(subject, CatalogPermissions.ProductsRead);
        using var before = ClientFor(subject, stamp: "stamp-1");
        Assert.Equal(HttpStatusCode.OK, (await before.GetAsync("/api/v1/products")).StatusCode);

        factory.GrantPermissions(subject);

        // A token carrying the old stamp still hits the cached entry; one issued after the change does not.
        using var sameStamp = ClientFor(subject, stamp: "stamp-1");
        using var newStamp = ClientFor(subject, stamp: "stamp-2");
        Assert.Equal(HttpStatusCode.OK, (await sameStamp.GetAsync("/api/v1/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await newStamp.GetAsync("/api/v1/products")).StatusCode);
    }

    [Fact]
    public async Task Removing_a_subjects_permissions_takes_effect_at_once()
    {
        var subject = Guid.NewGuid().ToString();
        var other = Guid.NewGuid().ToString();
        factory.GrantPermissions(subject, CatalogPermissions.ProductsRead);
        factory.GrantPermissions(other, CatalogPermissions.ProductsRead);
        using var client = ClientFor(subject, stamp: "stamp-1");
        using var otherClient = factory.CreateClientWithClaims(new Claim(NovaClaims.Subject, other));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await otherClient.GetAsync("/api/v1/products")).StatusCode);

        factory.GrantPermissions(subject);
        factory.GrantPermissions(other);
        await factory.Services.GetRequiredService<HybridCache>().RemovePermissionsAsync(subject, TestContext.Current.CancellationToken);

        // Whatever the stamp it was cached under; another subject's entry stays until it expires.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await otherClient.GetAsync("/api/v1/products")).StatusCode);
    }

    private HttpClient ClientFor(string subject, string stamp) =>
        factory.CreateClientWithClaims(new Claim(NovaClaims.Subject, subject), new Claim(NovaClaims.SecurityStamp, stamp));
}
