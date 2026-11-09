using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The sign-in rules every surface shares (PLAN F8.2, D-090 (2)): one outcome for an unknown account, a wrong password
/// and a locked one, lockout after five failures, the "confirm your email" outcome only after the right password, and
/// the two-factor step. The pages' own tests (<see cref="SignInHardeningTests"/>, <see cref="TwoFactorTests"/>) check
/// what the browser sees.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class SignInFlowTests(IdentityServerFixture fixture) : IClassFixture<IdentityServerFixture>
{
    private const string Password = "a long passphrase for tests";

    private readonly IdentityServerTestHost _host = fixture.Host;

    [Fact]
    public async Task An_unknown_account_is_invalid_credentials()
    {
        Assert.Equal(SignInOutcome.InvalidCredentials, await PasswordAsync("nobody@novacore.test", Password));
    }

    [Fact]
    public async Task A_wrong_password_is_invalid_credentials_and_counts_toward_lockout()
    {
        var user = await _host.CreateUserAsync("flow-wrong@novacore.test", Password);

        Assert.Equal(SignInOutcome.InvalidCredentials, await PasswordAsync(user.Email!, "not the right passphrase"));

        Assert.Equal(1, await AccessFailedCountAsync(user));
    }

    [Fact]
    public async Task A_locked_account_is_invalid_credentials_even_with_the_right_password()
    {
        var user = await _host.CreateUserAsync("flow-locked@novacore.test", Password);
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(SignInOutcome.InvalidCredentials, await PasswordAsync(user.Email!, "not the right passphrase"));

        Assert.True(await IsLockedOutAsync(user));
        Assert.Equal(SignInOutcome.InvalidCredentials, await PasswordAsync(user.Email!, Password));
    }

    [Fact]
    public async Task An_unconfirmed_account_hears_about_its_email_only_with_the_right_password()
    {
        var user = await CreateUnconfirmedUserAsync("flow-unconfirmed@novacore.test");

        Assert.Equal(SignInOutcome.InvalidCredentials, await PasswordAsync(user.Email!, "not the right passphrase"));
        Assert.Equal(1, await AccessFailedCountAsync(user));

        Assert.Equal(SignInOutcome.EmailNotConfirmed, await PasswordAsync(user.Email!, Password));
    }

    [Fact]
    public async Task An_account_with_two_factor_authentication_needs_the_code_step()
    {
        var user = await _host.CreateUserAsync("flow-2fa@novacore.test", Password);
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var stored = await users.FindByIdAsync(user.Id.ToString());
            await users.ResetAuthenticatorKeyAsync(stored!);
            await users.SetTwoFactorEnabledAsync(stored!, enabled: true);
        }

        Assert.Equal(SignInOutcome.TwoFactorRequired, await PasswordAsync(user.Email!, Password));
    }

    [Fact]
    public async Task The_code_step_without_the_password_step_has_no_session()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        UseRequest(scope.ServiceProvider);

        var outcome = await scope.ServiceProvider.GetRequiredService<SignInFlow>().CodeAsync("123456", ip: null);

        Assert.Equal(SignInOutcome.NoTwoFactorSession, outcome);
    }

    private async Task<SignInOutcome> PasswordAsync(string email, string password)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        UseRequest(scope.ServiceProvider);
        return await scope.ServiceProvider.GetRequiredService<SignInFlow>().PasswordAsync(email, password, ip: null);
    }

    // SignInManager signs in through an HttpContext; the scope's own one stands in for a request.
    private static void UseRequest(IServiceProvider services) =>
        services.GetRequiredService<SignInManager<ApplicationUser>>().Context = new DefaultHttpContext { RequestServices = services };

    private async Task<ApplicationUser> CreateUnconfirmedUserAsync(string email)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, CreatedAtUtc = TimeProvider.System.GetUtcNow() };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        return user;
    }

    private async Task<int> AccessFailedCountAsync(ApplicationUser user)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.GetAccessFailedCountAsync((await users.FindByIdAsync(user.Id.ToString()))!);
    }

    private async Task<bool> IsLockedOutAsync(ApplicationUser user)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.IsLockedOutAsync((await users.FindByIdAsync(user.Id.ToString()))!);
    }
}
