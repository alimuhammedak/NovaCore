using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Authorization;
using NovaCore.AspNetCore.Identity;
using NovaCore.Modules.Catalog.Authorization;
using NovaCore.Modules.Identity.Authorization;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Infrastructure;
using NovaCore.Modules.Identity.Services;
using NovaCore.Modules.Payments.Authorization;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// The Identity module's <see cref="IPermissionResolver"/> (PLAN F3a.2): a user's permissions are the <c>permission</c>
/// claims of every role they hold plus their own direct grants, read from the Identity store by the token's <c>sub</c>.
/// </summary>
public sealed class IdentityPermissionResolverTests(NovaCoreApiFactory factory)
    : IClassFixture<NovaCoreApiFactory>, IAsyncLifetime
{
    private readonly NovaCoreApiFactory _factory = factory;

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>().SeedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Role_permissions_and_direct_grants_are_combined()
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"{Guid.NewGuid():N}@novacore.test";
        var user = new ApplicationUser { UserName = email, Email = email, CreatedAtUtc = TimeProvider.System.GetUtcNow() };
        var created = await users.CreateAsync(user);
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, IdentityDataSeeder.UserRole);
        await users.AddClaimAsync(user, new Claim(NovaClaims.Permission, IdentityPermissions.UsersRead));

        var permissions = await Resolve(scope, new Claim(NovaClaims.Subject, user.Id.ToString()));

        // The User role holds the business modules' read permissions; the direct grant adds one more.
        Assert.Equal(
            [CatalogPermissions.ProductsRead, IdentityPermissions.UsersRead, PaymentsPermissions.Read],
            permissions.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("an unknown user id")]
    [InlineData("not a user id")]
    [InlineData(null)]
    public async Task A_caller_who_is_not_a_known_user_has_no_permissions(string? subject)
    {
        using var scope = _factory.Services.CreateScope();
        Claim[] claims = subject switch
        {
            null => [],
            "an unknown user id" => [new Claim(NovaClaims.Subject, Guid.NewGuid().ToString())],
            _ => [new Claim(NovaClaims.Subject, subject)]
        };

        var permissions = await Resolve(scope, claims);

        Assert.Empty(permissions);
    }

    private static Task<IReadOnlyCollection<string>> Resolve(IServiceScope scope, params Claim[] claims) =>
        scope.ServiceProvider.GetRequiredService<IPermissionResolver>()
            .GetPermissionsAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "test")), CancellationToken.None);
}
