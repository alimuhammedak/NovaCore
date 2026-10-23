using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Password reset (PLAN F4d.2): the sign-in page leads to a form that emails a reset link to a confirmed account, and the
/// link opens a form that sets a new password. The answers don't reveal which addresses have accounts; a reset ends every
/// session of the account and the link works once.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed partial class PasswordResetTests(PasswordResetTests.Fixture fixture) : IClassFixture<PasswordResetTests.Fixture>
{
    private const string Password = "a long passphrase for tests";
    private const string NewPassword = "a brand new passphrase";
    private const string InvalidLink = "This link is invalid or has expired.";

    private readonly IdentityServerTestHost _host = fixture.Host;

    public sealed class Fixture : IdentityServerFixture
    {
        public CapturingEmailSender Emails { get; } = new();

        protected override void Configure(WebApplicationBuilder builder) =>
            builder.Services.AddSingleton<IEmailSender>(Emails);
    }

    [Fact]
    public async Task A_reset_link_sets_a_new_password_and_ends_every_session()
    {
        await _host.CreateUserAsync("reset@novacore.test", Password);
        using var session = new OidcFlow(_host);
        var tokens = await session.SignInAsync("reset@novacore.test", Password);

        using var browser = new OidcFlow(_host);
        Assert.Equal(HttpStatusCode.OK, (await RequestResetAsync(browser, "reset@novacore.test")).StatusCode);
        var reset = await ResetAsync(browser, await ResetLinkAsync("reset@novacore.test"), NewPassword, NewPassword);

        Assert.Contains("Your password has been reset.", await reset.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal("invalid_grant", await OidcFlow.ErrorFrom(await session.RefreshAsync(tokens.RefreshToken)));

        using var fresh = new OidcFlow(_host);
        Assert.False(string.IsNullOrEmpty((await fresh.SignInAsync("reset@novacore.test", NewPassword)).AccessToken));
    }

    [Fact]
    public async Task A_reset_link_works_once()
    {
        await _host.CreateUserAsync("once@novacore.test", Password);
        using var browser = new OidcFlow(_host);
        await RequestResetAsync(browser, "once@novacore.test");
        var link = await ResetLinkAsync("once@novacore.test");

        await ResetAsync(browser, link, NewPassword, NewPassword);
        var again = await ResetAsync(browser, link, "yet another passphrase", "yet another passphrase");

        Assert.Contains(InvalidLink, await again.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        using var fresh = new OidcFlow(_host);
        Assert.False(string.IsNullOrEmpty((await fresh.SignInAsync("once@novacore.test", NewPassword)).AccessToken));
    }

    [Fact]
    public async Task An_unknown_or_unconfirmed_address_gets_the_same_answer_and_no_email()
    {
        await _host.CreateUserAsync("known-reset@novacore.test", Password);
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var unconfirmed = new ApplicationUser
            {
                UserName = "unconfirmed-reset@novacore.test",
                Email = "unconfirmed-reset@novacore.test",
                CreatedAtUtc = TimeProvider.System.GetUtcNow()
            };
            Assert.True((await users.CreateAsync(unconfirmed, Password)).Succeeded);
        }

        using var browser = new OidcFlow(_host);
        var known = await RequestResetAsync(browser, "known-reset@novacore.test");
        var unknown = await RequestResetAsync(browser, "nobody-reset@novacore.test");
        var notConfirmed = await RequestResetAsync(browser, "unconfirmed-reset@novacore.test");

        var expected = await known.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        Assert.Equal(expected, await unknown.Content.ReadAsStringAsync());
        Assert.Equal(expected, await notConfirmed.Content.ReadAsStringAsync());
        // The emails go out in the order queued: once a later one has gone, nothing queued before it is still pending.
        await _host.CreateUserAsync("after-reset@novacore.test", Password);
        await RequestResetAsync(browser, "after-reset@novacore.test");
        await fixture.Emails.WaitForAsync(email => string.Equals(email.To, "after-reset@novacore.test", StringComparison.Ordinal));
        Assert.Single(fixture.Emails.Sent, email => string.Equals(email.To, "known-reset@novacore.test", StringComparison.Ordinal));
        Assert.DoesNotContain(fixture.Emails.Sent, email => email.To is "nobody-reset@novacore.test" or "unconfirmed-reset@novacore.test");
    }

    [Fact]
    public async Task The_new_password_is_typed_twice()
    {
        await _host.CreateUserAsync("twice-reset@novacore.test", Password);
        using var browser = new OidcFlow(_host);
        await RequestResetAsync(browser, "twice-reset@novacore.test");

        var reset = await ResetAsync(browser, await ResetLinkAsync("twice-reset@novacore.test"), NewPassword, "a different passphrase");

        Assert.Contains("The passwords do not match.", await reset.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        using var flow = new OidcFlow(_host);
        Assert.False(string.IsNullOrEmpty((await flow.SignInAsync("twice-reset@novacore.test", Password)).AccessToken));
    }

    [Fact]
    public async Task The_password_policy_applies_to_the_new_password()
    {
        await _host.CreateUserAsync("policy-reset@novacore.test", Password);
        using var browser = new OidcFlow(_host);
        await RequestResetAsync(browser, "policy-reset@novacore.test");

        var reset = await ResetAsync(browser, await ResetLinkAsync("policy-reset@novacore.test"), "too-short", "too-short");

        var html = await reset.Content.ReadAsStringAsync();
        Assert.Contains("at least 15 characters", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Your password has been reset.", html, StringComparison.Ordinal);

        // A refused password leaves the link usable; a common one is refused too.
        var common = await ResetAsync(browser, await ResetLinkAsync("policy-reset@novacore.test"), "iloveyouforever", "iloveyouforever");
        Assert.Contains("too common", await common.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_bad_token_and_an_unknown_user_get_the_same_answer()
    {
        var user = await _host.CreateUserAsync("bad-token@novacore.test", Password);
        using var browser = new OidcFlow(_host);

        var badToken = await ResetAsync(browser, $"/account/reset-password?userId={user.Id}&token=bm90LWEtdG9rZW4", NewPassword, NewPassword);
        var unknownUser = await ResetAsync(browser, $"/account/reset-password?userId={Guid.NewGuid()}&token=bm90LWEtdG9rZW4", NewPassword, NewPassword);

        Assert.Equal(await badToken.Content.ReadAsStringAsync(), await unknownUser.Content.ReadAsStringAsync());
        Assert.Contains(InvalidLink, await badToken.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_reset_link_ignores_the_Host_header()
    {
        await _host.CreateUserAsync("hostile-reset@novacore.test", Password);
        using var browser = new OidcFlow(_host);

        var form = await browser.Browser.GetAsync(new Uri("/account/forgot-password", UriKind.Relative));
        await browser.SubmitFormAsync(form, "/account/forgot-password",
            new(StringComparer.Ordinal) { ["Email"] = "hostile-reset@novacore.test" },
            request => request.Headers.Host = "evil.example");

        Assert.StartsWith(IdentityServerTestHost.Authority + "/account/reset-password?", await ResetLinkAsync("hostile-reset@novacore.test"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_sign_in_page_leads_to_the_reset_form()
    {
        using var browser = new OidcFlow(_host);

        var html = await (await browser.Browser.GetAsync(new Uri("/account/login", UriKind.Relative))).Content.ReadAsStringAsync();

        Assert.Contains("href=\"/account/forgot-password\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/account/forgot-password")]
    [InlineData("/account/reset-password")]
    [InlineData("/api/v1/auth/resend-confirmation-email")]
    public void The_new_account_endpoints_use_the_strict_rate_limit(string path)
    {
        var endpoints = _host.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => string.Equals(endpoint.RoutePattern.RawText?.TrimEnd('/'), path, StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, endpoint =>
            Assert.Equal("strict", endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName));
    }

    private static async Task<HttpResponseMessage> RequestResetAsync(OidcFlow browser, string email)
    {
        var form = await browser.Browser.GetAsync(new Uri("/account/forgot-password", UriKind.Relative));
        return await browser.SubmitFormAsync(form, "/account/forgot-password", new(StringComparer.Ordinal) { ["Email"] = email });
    }

    private static async Task<HttpResponseMessage> ResetAsync(OidcFlow browser, string link, string password, string confirmation)
    {
        var target = new Uri(link, UriKind.RelativeOrAbsolute);
        var form = await browser.Browser.GetAsync(target.IsAbsoluteUri ? new Uri(target.PathAndQuery, UriKind.Relative) : target);
        return await browser.SubmitFormAsync(form, "/account/reset-password", new(StringComparer.Ordinal)
        {
            ["Password"] = password,
            ["ConfirmPassword"] = confirmation
        });
    }

    private async Task<string> ResetLinkAsync(string email)
    {
        var body = Assert.Single(await fixture.Emails.WaitForAsync(sent => string.Equals(sent.To, email, StringComparison.Ordinal))).HtmlBody;
        return WebUtility.HtmlDecode(Href().Match(body).Groups["url"].Value);
    }

    [GeneratedRegex("href=\"(?<url>[^\"]+)\"", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Href();
}
