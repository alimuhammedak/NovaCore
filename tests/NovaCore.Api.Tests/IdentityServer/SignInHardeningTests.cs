using System.Buffers.Binary;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The account hardening carried over from the Identity module (PLAN F3b.2): PBKDF2 with 600,000 iterations, a
/// length-first password policy, unique emails, lockout after five failures, confirmed email before sign-in, a sign-in
/// page that doesn't reveal which accounts exist, and an audit log that masks emails and never holds a password.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class SignInHardeningTests(SignInHardeningTests.Fixture fixture) : IClassFixture<SignInHardeningTests.Fixture>
{
    private const string Password = "a long passphrase for tests";
    private const string InvalidCredentials = "Invalid email or password.";

    private readonly IdentityServerTestHost _host = fixture.Host;

    public sealed class Fixture : IdentityServerFixture
    {
        public LogCapture Log { get; } = new();

        protected override void Configure(WebApplicationBuilder builder) => builder.Logging.AddProvider(Log);
    }

    [Fact]
    public async Task Passwords_are_hashed_with_600000_PBKDF2_iterations()
    {
        var user = await _host.CreateUserAsync("hash@novacore.test", Password);

        // Identity's version 3 format: 0x01, then the PRF and the iteration count as big-endian integers.
        var hash = Convert.FromBase64String(user.PasswordHash!);
        Assert.Equal(0x01, hash[0]);
        Assert.Equal(600_000u, BinaryPrimitives.ReadUInt32BigEndian(hash.AsSpan(5, 4)));
    }

    [Theory]
    [InlineData("short-pass1", false)]            // 11 characters
    [InlineData("onlylowercaseletters", true)]    // long enough, no composition rules
    public async Task The_password_policy_asks_for_length_not_composition(string password, bool accepted)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"{Guid.NewGuid():N}@novacore.test";

        var result = await users.CreateAsync(NewUser(email), password);

        Assert.Equal(accepted, result.Succeeded);
    }

    [Fact]
    public async Task Email_addresses_are_unique()
    {
        await _host.CreateUserAsync("unique@novacore.test", Password);
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var second = NewUser("unique@novacore.test");
        second.UserName = "another-name";
        var result = await users.CreateAsync(second, Password);

        Assert.Contains(result.Errors, error => error.Code == "DuplicateEmail");
    }

    [Fact]
    public async Task Five_failed_sign_ins_lock_the_account()
    {
        await _host.CreateUserAsync("lockout@novacore.test", Password);
        using var flow = new OidcFlow(_host);

        for (var attempt = 0; attempt < 5; attempt++)
            await SignInPageAsync(flow, "lockout@novacore.test", "wrong password here");

        // Even the right password is refused now, with the same message.
        var response = await SignInPageAsync(flow, "lockout@novacore.test", Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(InvalidCredentials, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Contains(fixture.Log.Messages, message => message.StartsWith("SECURITY account_locked_out", StringComparison.Ordinal));
    }

    [Fact]
    public async Task An_unconfirmed_account_cannot_sign_in()
    {
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.CreateAsync(NewUser("unconfirmed@novacore.test"), Password)).Succeeded);
        }

        using var flow = new OidcFlow(_host);
        var response = await SignInPageAsync(flow, "unconfirmed@novacore.test", Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Confirm your email address", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_wrong_password_on_an_unconfirmed_account_gets_the_generic_message_and_counts()
    {
        ApplicationUser user;
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            user = NewUser("unconfirmed-wrong@novacore.test");
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        }

        using var flow = new OidcFlow(_host);
        var response = await SignInPageAsync(flow, "unconfirmed-wrong@novacore.test", "not the password");

        // The "confirm your email" hint would tell anyone that the address is registered.
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(InvalidCredentials, html, StringComparison.Ordinal);
        Assert.DoesNotContain("Confirm your email address", html, StringComparison.Ordinal);

        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.Equal(1, await users.GetAccessFailedCountAsync((await users.FindByIdAsync(user.Id.ToString()))!));
        }
    }

    [Fact]
    public async Task An_unknown_account_and_a_wrong_password_look_the_same()
    {
        await _host.CreateUserAsync("known@novacore.test", Password);
        using var flow = new OidcFlow(_host);

        var unknown = await SignInPageAsync(flow, "unknown@novacore.test", Password);
        var wrongPassword = await SignInPageAsync(flow, "known@novacore.test", "not the password");

        Assert.Equal(unknown.StatusCode, wrongPassword.StatusCode);
        Assert.Contains(InvalidCredentials, await unknown.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Contains(InvalidCredentials, await wrongPassword.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failed_sign_in_is_audited_with_a_masked_email_and_no_password()
    {
        await _host.CreateUserAsync("audit@novacore.test", Password);
        using var flow = new OidcFlow(_host);

        await SignInPageAsync(flow, "audit@novacore.test", "a secret wrong password");

        var entry = Assert.Single(fixture.Log.Messages, message =>
            message.StartsWith("SECURITY login_failed Email=a***@novacore.test", StringComparison.Ordinal));
        Assert.Contains("Reason=bad-password", entry, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.Log.Messages, message =>
            message.Contains("a secret wrong password", StringComparison.Ordinal)
            || message.Contains("audit@novacore.test", StringComparison.Ordinal));
    }

    private static async Task<HttpResponseMessage> SignInPageAsync(OidcFlow flow, string email, string password) =>
        await flow.SubmitLoginAsync(await flow.Browser.GetAsync("/account/login?ReturnUrl=%2F"), email, password);

    private static ApplicationUser NewUser(string email) =>
        new() { UserName = email, Email = email, CreatedAtUtc = TimeProvider.System.GetUtcNow() };
}
