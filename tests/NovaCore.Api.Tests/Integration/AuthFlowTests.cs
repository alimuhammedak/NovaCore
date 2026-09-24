using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using NovaCore.Modules.Identity.Domain;
using NovaCore.Modules.Identity.Infrastructure;
using NovaCore.Modules.Identity.Services;
using Xunit;

namespace NovaCore.Api.Tests.Integration;

/// <summary>
/// End-to-end security tests against the real host with a seeded in-memory Identity store: login,
/// the rotating-refresh-token flow, OWASP reuse detection, and permission enforcement.
/// </summary>
public class AuthFlowTests(NovaCoreApiFactory factory) : IClassFixture<NovaCoreApiFactory>, IAsyncLifetime
{
    private const string AdminEmail = "admin@novacore.test";
    private const string AdminPassword = "Admin!Passw0rd-2026";
    private const string UserEmail = "user@novacore.test";
    private const string UserPassword = "User!Passw0rd-2026";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly NovaCoreApiFactory _factory = factory;

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        await sp.GetRequiredService<IdentityDbContext>().Database.EnsureCreatedAsync();
        await sp.GetRequiredService<IdentityDataSeeder>().SeedAsync();

        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(users, AdminEmail, AdminPassword, IdentityDataSeeder.AdministratorRole);
        await EnsureUserAsync(users, UserEmail, UserPassword, IdentityDataSeeder.UserRole);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Login_with_valid_credentials_returns_a_token_pair()
    {
        var client = _factory.CreateClient();

        var tokens = await LoginAsync(client, AdminEmail, AdminPassword);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.Equal("Bearer", tokens.TokenType);
        Assert.True(tokens.ExpiresInSeconds > 0);
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_generic()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = AdminEmail, password = "totally-wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_reflects_roles_for_the_authenticated_user()
    {
        var client = _factory.CreateClient();
        var tokens = await LoginAsync(client, AdminEmail, AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var me = await client.GetFromJsonAsync<MeDto>("/api/v1/auth/me", Json);

        Assert.Equal(AdminEmail, me!.Email);
        Assert.Contains(IdentityDataSeeder.AdministratorRole, me.Roles);
    }

    [Fact]
    public async Task Refresh_rotates_the_refresh_token()
    {
        var client = _factory.CreateClient();
        var tokens = await LoginAsync(client, AdminEmail, AdminPassword);

        var refreshed = await RefreshAsync(client, tokens.RefreshToken);

        Assert.NotNull(refreshed);
        Assert.NotEqual(tokens.RefreshToken, refreshed!.RefreshToken); // rotated
        Assert.False(string.IsNullOrWhiteSpace(refreshed.AccessToken));
    }

    [Fact]
    public async Task Reusing_a_rotated_refresh_token_is_detected_and_revokes_the_family()
    {
        var client = _factory.CreateClient();
        var tokens = await LoginAsync(client, AdminEmail, AdminPassword);

        // First refresh succeeds and rotates the token.
        var rotated = await RefreshAsync(client, tokens.RefreshToken);
        Assert.NotNull(rotated);

        // Replaying the now-consumed original token is reuse → 401.
        var reuseResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        // ...and reuse detection revokes the whole family, so the rotated token is dead too.
        var afterFamilyRevoke = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = rotated!.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterFamilyRevoke.StatusCode);
    }

    [Fact]
    public async Task Admin_can_read_users_but_a_standard_user_cannot_manage_them()
    {
        var client = _factory.CreateClient();

        // Admin (super-admin) can list users.
        var adminTokens = await LoginAsync(client, AdminEmail, AdminPassword);
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens.AccessToken);
        var adminList = await adminClient.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, adminList.StatusCode);

        // Standard user lacks identity:users:manage → 403 on a management action.
        var userTokens = await LoginAsync(client, UserEmail, UserPassword);
        var userClient = _factory.CreateClient();
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userTokens.AccessToken);
        var manage = await userClient.PostAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/roles", new { role = "User" });
        Assert.Equal(HttpStatusCode.Forbidden, manage.StatusCode);
    }

    private async Task<TokenPair> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TokenPair>(Json))!;
    }

    private static async Task<TokenPair?> RefreshAsync(HttpClient client, string refreshToken)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<TokenPair>(Json);
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> users, string email, string password, string role)
    {
        if (await users.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, role);
    }

    private sealed record TokenPair(string AccessToken, string TokenType, int ExpiresInSeconds, string RefreshToken);
    private sealed record MeDto(string UserId, string? UserName, string? Email, string[] Roles, string[] Permissions);
}
