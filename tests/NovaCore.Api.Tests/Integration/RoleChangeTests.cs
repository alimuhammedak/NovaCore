using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.AspNetCore.Identity;
using NovaCore.Modules.Identity.Authorization;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Infrastructure;
using NovaCore.Modules.Identity.Services;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// PLAN F3a.2's acceptance test: a role change reaches a signed-in Identity user within <c>Cache:PermissionExpiration</c>.
/// It has its own host because the Identity module stamps its tokens from the app's (fake) clock while JWT validation
/// reads the real one (R-058): a clock another test had already advanced would make the login token not yet valid.
/// </summary>
public sealed class RoleChangeTests(FakeClockApiFactory factory) : IClassFixture<FakeClockApiFactory>, IAsyncLifetime
{
    // Cache:PermissionExpiration's default; the test host leaves it unset.
    private static readonly TimeSpan PermissionExpiration = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async ValueTask InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>().SeedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task A_role_change_reaches_a_signed_in_user_within_the_cache_lifetime()
    {
        var (email, password) = await CreateUserAsync(IdentityDataSeeder.UserRole);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, password));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/users")).StatusCode);

        await AddToRoleWithPermissionAsync(email, "Auditor", IdentityPermissions.UsersRead);

        // Same token, still inside the cache lifetime: the old permissions stand...
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/users")).StatusCode);

        // ...and once it has passed, the new role's permission is resolved.
        factory.Clock.Advance(PermissionExpiration + TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/users")).StatusCode);
    }

    private async Task<(string Email, string Password)> CreateUserAsync(string role)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"{Guid.NewGuid():N}@novacore.test";
        const string password = "Role!Change-Passw0rd";
        var user = new ApplicationUser
        {
            UserName = email, Email = email, EmailConfirmed = true, CreatedAtUtc = factory.Clock.GetUtcNow()
        };
        var created = await users.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, role);
        return (email, password);
    }

    private async Task AddToRoleWithPermissionAsync(string email, string roleName, string permission)
    {
        using var scope = factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var role = new ApplicationRole(roleName);
        Assert.True((await roles.CreateAsync(role)).Succeeded);
        await roles.AddClaimAsync(role, new Claim(NovaClaims.Permission, permission));
        Assert.True((await users.AddToRoleAsync((await users.FindByEmailAsync(email))!, roleName)).Succeeded);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TokenPair>(Json))!.AccessToken;
    }

    private sealed record TokenPair(string AccessToken);
}
