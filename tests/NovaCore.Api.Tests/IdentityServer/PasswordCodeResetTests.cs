using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Password reset with a code for a project's own screen (PLAN F8.5, D-091 (4)): every address gets the same answer, a
/// code works once, for 15 minutes and 5 wrong tries, a run of 100 wrong codes stops codes until the owner signs in, and a
/// reset ends every session without signing anyone in.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed partial class PasswordCodeResetTests(PasswordCodeResetTests.Fixture fixture) : IClassFixture<PasswordCodeResetTests.Fixture>
{
    private const string Password = "a long passphrase for tests";
    private const string NewPassword = "a brand new passphrase";
    private const string Forgot = "/api/v1/auth/password/forgot";
    private const string Reset = "/api/v1/auth/password/reset";

    private readonly IdentityServerTestHost _host = fixture.Host;

    public sealed class Fixture : IdentityServerFixture
    {
        public CapturingEmailSender Emails { get; } = new();

        protected override void Configure(WebApplicationBuilder builder) => builder.Services.AddSingleton<IEmailSender>(Emails);
    }

    [Fact]
    public async Task Every_address_gets_the_same_answer_and_only_a_confirmed_account_gets_a_code()
    {
        await _host.CreateUserAsync("code-known@novacore.test", Password);
        await CreateUnconfirmedAsync(_host, "code-unconfirmed@novacore.test");
        using var client = _host.CreateClient();

        var answers = new[]
        {
            await PostAsync(client, Forgot, new { email = "code-nobody@novacore.test" }),
            await PostAsync(client, Forgot, new { email = "code-unconfirmed@novacore.test" }),
            await PostAsync(client, Forgot, new { email = "code-known@novacore.test" }),
        };

        Assert.All(answers, answer => Assert.Equal(HttpStatusCode.Accepted, answer.StatusCode));
        Assert.Single((await Task.WhenAll(answers.Select(answer => answer.Content.ReadAsStringAsync()))).Distinct(StringComparer.Ordinal));

        // The confirmed account's email was queued last; once it is out, the others would have gone before it.
        await fixture.Emails.WaitForAsync(email => string.Equals(email.To, "code-known@novacore.test", StringComparison.Ordinal));
        Assert.DoesNotContain(fixture.Emails.Sent, email => email.To is "code-nobody@novacore.test" or "code-unconfirmed@novacore.test");
    }

    [Fact]
    public async Task A_code_resets_the_password_once_ends_every_session_and_signs_nobody_in()
    {
        await _host.CreateUserAsync("code-reset@novacore.test", Password);
        using var session = new OidcFlow(_host);
        var tokens = await session.SignInAsync("code-reset@novacore.test", Password);
        using var client = _host.CreateClient();
        var code = await RequestCodeAsync(client, "code-reset@novacore.test");

        var reset = await PostAsync(client, Reset, new { email = "code-reset@novacore.test", code, newPassword = NewPassword });

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.False(reset.Headers.Contains("Set-Cookie"));
        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(await session.RefreshAsync(tokens.RefreshToken)));
        using (var fresh = new OidcFlow(_host))
            Assert.False(string.IsNullOrEmpty((await fresh.SignInAsync("code-reset@novacore.test", NewPassword)).AccessToken));

        var again = await PostAsync(client, Reset, new { email = "code-reset@novacore.test", code, newPassword = "yet another passphrase" });
        await AssertInvalidCodeAsync(again);
    }

    [Fact]
    public async Task After_five_wrong_tries_the_right_code_no_longer_works()
    {
        var user = await _host.CreateUserAsync("code-tries@novacore.test", Password);
        using var client = _host.CreateClient();
        var code = await RequestCodeAsync(client, user.Email!);

        for (var attempt = 0; attempt < PasswordResetCodes.MaxTriesPerCode; attempt++)
            await AssertInvalidCodeAsync(await PostAsync(client, Reset, new { email = user.Email, code = Wrong(code), newPassword = NewPassword }));

        await AssertInvalidCodeAsync(await PostAsync(client, Reset, new { email = user.Email, code, newPassword = NewPassword }));
    }

    [Fact]
    public async Task A_wrong_code_does_not_lock_the_account()
    {
        var user = await _host.CreateUserAsync("code-nolock@novacore.test", Password);
        using var client = _host.CreateClient();
        var code = await RequestCodeAsync(client, user.Email!);

        for (var attempt = 0; attempt < PasswordResetCodes.MaxTriesPerCode; attempt++)
            await PostAsync(client, Reset, new { email = user.Email, code = Wrong(code), newPassword = NewPassword });

        using var browser = new OidcFlow(_host);
        Assert.False(string.IsNullOrEmpty((await browser.SignInAsync(user.Email!, Password)).AccessToken));
    }

    [Fact]
    public async Task A_new_code_replaces_the_old_one()
    {
        var user = await _host.CreateUserAsync("code-replaced@novacore.test", Password);
        using var client = _host.CreateClient();
        var first = await RequestCodeAsync(client, user.Email!);
        var second = await RequestCodeAsync(client, user.Email!, previous: 1);

        if (!string.Equals(first, second, StringComparison.Ordinal))
            await AssertInvalidCodeAsync(await PostAsync(client, Reset, new { email = user.Email, code = first, newPassword = NewPassword }));
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, Reset, new { email = user.Email, code = second, newPassword = NewPassword })).StatusCode);
    }

    [Fact]
    public async Task After_a_run_of_wrong_codes_codes_stop_working_until_the_owner_signs_in()
    {
        var user = await _host.CreateUserAsync("code-run@novacore.test", Password);
        await SetFailuresAsync(user, PasswordResetCodes.MaxConsecutiveFailures - 1);
        using var client = _host.CreateClient();

        var code = await RequestCodeAsync(client, user.Email!);
        await AssertInvalidCodeAsync(await PostAsync(client, Reset, new { email = user.Email, code = Wrong(code), newPassword = NewPassword }));
        await AssertInvalidCodeAsync(await PostAsync(client, Reset, new { email = user.Email, code, newPassword = NewPassword }));

        using (var browser = new OidcFlow(_host))
            await browser.SignInAsync(user.Email!, Password);

        var afterSignIn = await RequestCodeAsync(client, user.Email!, previous: 1);
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(client, Reset, new { email = user.Email, code = afterSignIn, newPassword = NewPassword })).StatusCode);
    }

    [Fact]
    public async Task An_unknown_or_unconfirmed_account_answers_like_a_wrong_code()
    {
        await CreateUnconfirmedAsync(_host, "code-reset-unconfirmed@novacore.test");
        using var client = _host.CreateClient();

        await AssertInvalidCodeAsync(await PostAsync(client, Reset, new { email = "code-reset-nobody@novacore.test", code = "12345678", newPassword = NewPassword }));
        await AssertInvalidCodeAsync(await PostAsync(client, Reset, new { email = "code-reset-unconfirmed@novacore.test", code = "12345678", newPassword = NewPassword }));
    }

    // Its own host: the clock moves, and this test signs nobody in (tests/CLAUDE.md).
    [Fact]
    public async Task An_expired_code_is_refused()
    {
        var clock = new FakeTimeProvider(TimeProvider.System.GetUtcNow());
        var emails = new CapturingEmailSender();
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
        {
            builder.Services.AddSingleton<TimeProvider>(clock);
            builder.Services.AddSingleton<IEmailSender>(emails);
        });
        await host.CreateUserAsync("code-expired@novacore.test", Password);
        using var client = host.CreateClient();
        await PostAsync(client, Forgot, new { email = "code-expired@novacore.test" });
        var code = CodeIn((await emails.WaitForAsync(email => string.Equals(email.To, "code-expired@novacore.test", StringComparison.Ordinal)))[0]);

        clock.Advance(PasswordResetCodes.Lifetime + TimeSpan.FromSeconds(1));

        await AssertInvalidCodeAsync(await PostAsync(client, Reset, new { email = "code-expired@novacore.test", code, newPassword = NewPassword }));
    }

    private async Task<string> RequestCodeAsync(HttpClient client, string email, int previous = 0)
    {
        Assert.Equal(HttpStatusCode.Accepted, (await PostAsync(client, Forgot, new { email })).StatusCode);
        var sent = await fixture.Emails.WaitForAsync(message => string.Equals(message.To, email, StringComparison.Ordinal), previous + 1);
        return CodeIn(sent[previous]);
    }

    private async Task SetFailuresAsync(ApplicationUser user, int failures)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var stored = (await users.FindByIdAsync(user.Id.ToString()))!;
        await users.SetAuthenticationTokenAsync(stored, PasswordResetCodes.LoginProvider, PasswordResetCodes.FailuresToken,
            failures.ToString(CultureInfo.InvariantCulture));
    }

    private static async Task CreateUnconfirmedAsync(IdentityServerTestHost host, string email)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var created = await users.CreateAsync(
            new ApplicationUser { UserName = email, Email = email, CreatedAtUtc = TimeProvider.System.GetUtcNow() }, Password);
        Assert.True(created.Succeeded);
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, object body) =>
        await client.PostAsJsonAsync(new Uri(path, UriKind.Relative), body, TestContext.Current.CancellationToken);

    private static async Task AssertInvalidCodeAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("account.invalid_reset_code", body.RootElement.GetProperty("code").GetString());
    }

    private static string Wrong(string code) =>
        ((int.Parse(code, CultureInfo.InvariantCulture) + 1) % 100_000_000).ToString("D8", CultureInfo.InvariantCulture);

    private static string CodeIn(SentEmail email) => Code().Match(email.HtmlBody).Groups["code"].Value;

    [GeneratedRegex(@"<strong>(?<code>\d{8})</strong>", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Code();
}
