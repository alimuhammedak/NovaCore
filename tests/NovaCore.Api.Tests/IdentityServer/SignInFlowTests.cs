using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The sign-in rules every surface shares (PLAN F8.2, D-090 (2)): one outcome for an unknown account, a wrong password
/// and a locked one, lockout after five failures, the "confirm your email" outcome only after the right password, and
/// the two-factor step, and one password hash for every refusal. The pages' own tests (<see cref="SignInHardeningTests"/>, <see cref="TwoFactorTests"/>) check
/// what the browser sees.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class SignInFlowTests(SignInHardeningTests.Fixture fixture) : IClassFixture<SignInHardeningTests.Fixture>
{
    private const string Password = "a long passphrase for tests";
    private const string WrongPassword = "not the right passphrase";

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

    // The answers are alike (above) and so is their cost: one password hash whichever account it is, so the time of the
    // answer doesn't tell a locked or unconfirmed account from an unknown one (D-030 (1), D-091 (1)). SignInManager
    // answers a locked or unconfirmed account before it hashes the password (docs/plan/RESEARCH.md R-138).
    [Fact]
    public async Task Every_refusal_costs_the_same_password_hashes()
    {
        var wrong = await _host.CreateUserAsync("flow-cost-wrong@novacore.test", Password);
        var locked = await _host.CreateUserAsync("flow-cost-locked@novacore.test", Password);
        await LockOutAsync(locked);
        var unconfirmed = await CreateUnconfirmedUserAsync("flow-cost-unconfirmed@novacore.test");
        var lockedUnconfirmed = await CreateUnconfirmedUserAsync("flow-cost-locked-unconfirmed@novacore.test");
        await LockOutAsync(lockedUnconfirmed);

        // The first check for an unknown address makes the dummy hash, once per host.
        await PasswordAsync("flow-cost-warm-up@novacore.test", WrongPassword);

        var costs = new[]
        {
            await PasswordHashesOfAsync("flow-cost-nobody@novacore.test", WrongPassword),
            await PasswordHashesOfAsync(wrong.Email!, WrongPassword),
            await PasswordHashesOfAsync(locked.Email!, Password),
            await PasswordHashesOfAsync(unconfirmed.Email!, WrongPassword),
            await PasswordHashesOfAsync(lockedUnconfirmed.Email!, Password),
        };

        Assert.Equal([1, 1, 1, 1, 1], costs.Select(cost => cost.Hashes));
        Assert.All(costs, cost => Assert.Equal(SignInOutcome.InvalidCredentials, cost.Outcome));
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

    private async Task<(SignInOutcome Outcome, int Hashes)> PasswordHashesOfAsync(string email, string password)
    {
        var before = fixture.PasswordChecks.Count;
        var outcome = await PasswordAsync(email, password);
        return (outcome, fixture.PasswordChecks.Count - before);
    }

    private async Task LockOutAsync(ApplicationUser user)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var stored = (await users.FindByIdAsync(user.Id.ToString()))!;
        Assert.True((await users.SetLockoutEndDateAsync(stored, TimeProvider.System.GetUtcNow().AddHours(1))).Succeeded);
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
