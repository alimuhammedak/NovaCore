using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The account hardening carried over from the Identity module (PLAN F3b.2): PBKDF2 with 600,000 iterations, a
/// length-first password policy, unique emails, lockout after five failures, confirmed email before sign-in, a sign-in
/// page that doesn't reveal which accounts exist, by its answer or by its time (PLAN F4d.4), and an audit log that masks
/// emails and never holds a password.
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

        public CountingPasswordHasher.Counter PasswordChecks { get; } = new();

        protected override void Configure(WebApplicationBuilder builder)
        {
            builder.Logging.AddProvider(Log);
            builder.Services.AddSingleton(PasswordChecks);
            builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, CountingPasswordHasher>();
        }
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

    // NIST SP 800-63B-4: at least 15 characters for a password that can be a single factor, no composition rules.
    [Theory]
    [InlineData("short-pass1", false)]            // 11 characters
    [InlineData("fourteen-chars", false)]         // 14 characters
    [InlineData("fifteen-letters", true)]         // 15 characters
    [InlineData("onlylowercaseletters", true)]    // long enough, no composition rules
    public async Task The_password_policy_asks_for_length_not_composition(string password, bool accepted)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"{Guid.NewGuid():N}@novacore.test";

        var result = await users.CreateAsync(NewUser(email), password);

        Assert.Equal(accepted, result.Succeeded);
    }

    // NIST SP 800-63B-4: a password from a list of common ones is refused, whatever its length; compared ignoring case.
    [Theory]
    [InlineData("1qaz2wsx3edc4rfv")]
    [InlineData("123456789qwerty")]
    [InlineData("QAZWSXEDCRFVTGB")]
    public async Task A_common_password_is_refused(string password)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var result = await users.CreateAsync(NewUser($"{Guid.NewGuid():N}@novacore.test"), password);

        Assert.Contains(result.Errors, error => string.Equals(error.Code, "CommonPassword", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Registration_refuses_a_common_password()
    {
        using var client = _host.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "common-password@novacore.test", password = "iloveyouforever" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("too common", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
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

        Assert.Contains(result.Errors, error => string.Equals(error.Code, "DuplicateEmail", StringComparison.Ordinal));
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
    public async Task An_unknown_account_costs_a_password_check_like_a_known_one()
    {
        await _host.CreateUserAsync("timing@novacore.test", Password);
        using var flow = new OidcFlow(_host);

        var before = fixture.PasswordChecks.Count;
        await SignInPageAsync(flow, "timing@novacore.test", "not the password");
        var known = fixture.PasswordChecks.Count - before;

        before = fixture.PasswordChecks.Count;
        await SignInPageAsync(flow, "nobody-timing@novacore.test", "not the password");
        var unknown = fixture.PasswordChecks.Count - before;

        // The hash is what takes the time (600,000 iterations), so the answer doesn't come sooner for an unknown address.
        Assert.Equal(1, known);
        Assert.Equal(known, unknown);
    }

    [Fact]
    public async Task A_lockout_from_wrong_current_passwords_is_audited()
    {
        var user = await _host.CreateUserAsync("change-audit@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var tokens = await flow.SignInAsync("change-audit@novacore.test", Password);

        for (var attempt = 0; attempt < 5; attempt++)
            await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/change-password", tokens.AccessToken,
                new { currentPassword = $"wrong guess number {attempt}", newPassword = "another long passphrase" });

        Assert.Contains(fixture.Log.Messages, message =>
            message.StartsWith($"SECURITY account_locked_out User={user.Id}", StringComparison.Ordinal));
    }

    // Y-35 and CodeQL cs/exposure-of-sensitive-information (F6b.6d): the audit names the account by its id, never by its
    // address, not even a masked one; an unknown address leaves no trace of itself.
    [Fact]
    public async Task A_failed_sign_in_is_audited_by_account_id_without_the_email_or_the_password()
    {
        var user = await _host.CreateUserAsync("audit@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        // The class's other tests fail sign-ins for unknown addresses too.
        var unknownBefore = AuditMessages().Count(message => message.Contains("Reason=unknown-user", StringComparison.Ordinal));

        await SignInPageAsync(flow, "audit@novacore.test", "a secret wrong password");
        using var unknown = new OidcFlow(_host);
        await SignInPageAsync(unknown, "nobody-audit@novacore.test", "a secret wrong password");

        var entry = Assert.Single(fixture.Log.Messages, message =>
            message.StartsWith($"SECURITY login_failed User={user.Id}", StringComparison.Ordinal));
        Assert.Contains("Reason=bad-password", entry, StringComparison.Ordinal);
        Assert.Equal(unknownBefore + 1, AuditMessages().Count(message => message.Contains("Reason=unknown-user", StringComparison.Ordinal)));
        Assert.DoesNotContain(fixture.Log.Messages, message => message.Contains("a secret wrong password", StringComparison.Ordinal));
        Assert.DoesNotContain(AuditMessages(), message =>
            message.Contains("@novacore.test", StringComparison.Ordinal) || message.Contains("***", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_registration_is_audited_without_the_email()
    {
        using var client = _host.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/register", new { email = "registered-audit@novacore.test", password = Password });

        Assert.Single(fixture.Log.Messages, message => message.StartsWith("SECURITY user_registered", StringComparison.Ordinal));
        // The Development email sender logs the address on purpose (it stands in for the mail server); the audit doesn't.
        Assert.DoesNotContain(AuditMessages(), message =>
            message.Contains("registered-audit", StringComparison.Ordinal) || message.Contains("r***@", StringComparison.Ordinal));
    }

    private IEnumerable<string> AuditMessages() =>
        fixture.Log.Messages.Where(message => message.StartsWith("SECURITY ", StringComparison.Ordinal));

    // CodeQL cs/user-controlled-bypass on LoginPage (F6b.6d, a false positive): the request's content type, which the
    // caller controls, can only turn a form post away. A valid antiforgery token sent in the header with another content
    // type is still a bad request, neither a sign-in nor a 500 from reading a form that isn't one.
    [Theory]
    [InlineData("/account/login")]
    [InlineData("/account/login/2fa")]
    public async Task A_post_that_is_not_a_form_is_a_bad_request_even_with_a_valid_antiforgery_token(string path)
    {
        await _host.CreateUserAsync($"not-a-form-{Guid.NewGuid():N}@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var page = await flow.Browser.GetAsync(new Uri("/account/login", UriKind.Relative));
        var token = OidcFlow.HiddenValue(await page.Content.ReadAsStringAsync(), "__RequestVerificationToken");

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new { Email = "x@novacore.test", Password, Code = "123456" })
        };
        request.Headers.Add("RequestVerificationToken", token);
        var response = await flow.Browser.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> SignInPageAsync(OidcFlow flow, string email, string password) =>
        await flow.SubmitLoginAsync(await flow.Browser.GetAsync(new Uri("/account/login?ReturnUrl=%2F", UriKind.Relative)), email, password);

    private static ApplicationUser NewUser(string email) =>
        new() { UserName = email, Email = email, CreatedAtUtc = TimeProvider.System.GetUtcNow() };
}

/// <summary>Identity's password hasher, counting its checks.</summary>
public sealed class CountingPasswordHasher(IOptions<PasswordHasherOptions> options, CountingPasswordHasher.Counter counter)
    : PasswordHasher<ApplicationUser>(options)
{
    public override PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        counter.Increment();
        return base.VerifyHashedPassword(user, hashedPassword, providedPassword);
    }

    public sealed class Counter
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Increment() => Interlocked.Increment(ref _count);
    }
}
