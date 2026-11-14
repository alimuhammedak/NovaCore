using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The anonymous endpoints that email an address (registration, resending the confirmation, the reset link and the
/// reset code) share a limit per recipient address on top of the per-IP one (S-6, docs/plan/RESEARCH.md R-157): a caller
/// rotating client addresses can't mail-bomb one mailbox, end its owner's outstanding reset code or fill the email queue
/// for it. The limit counts the requested address whether or not it has an account, so its 429 reveals no account.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed partial class RecipientEmailLimitTests(RecipientEmailLimitTests.Fixture fixture)
    : IClassFixture<RecipientEmailLimitTests.Fixture>
{
    private const int Limit = 3;
    private const string Password = "a long passphrase for tests";
    private const string NewPassword = "a brand new passphrase";
    private const string Forgot = "/api/v1/auth/password/forgot";

    private readonly IdentityServerTestHost _host = fixture.Host;

    public sealed class Fixture : IdentityServerFixture
    {
        public CapturingEmailSender Emails { get; } = new();

        protected override void Configure(WebApplicationBuilder builder)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Identity:EmailLimit:PermitLimit"] = Limit.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
            builder.Services.AddSingleton<IEmailSender>(Emails);
        }
    }

    [Fact]
    public async Task Requests_for_one_address_from_many_client_addresses_stop_after_the_limit_and_keep_the_last_code()
    {
        const string victim = "limit-victim@novacore.test";
        await _host.CreateUserAsync(victim, Password);

        // A new /64 for every request: neither the per-IP partition nor the /64 one counts them together.
        for (var i = 1; i <= Limit; i++)
            Assert.Equal(StatusCodes.Status202Accepted, (await PostFromAsync(Forgot, new { email = victim }, $"2001:db8:{i}::1")).Status);
        var sent = await fixture.Emails.WaitForAsync(email => string.Equals(email.To, victim, StringComparison.Ordinal), Limit);
        var lastCode = CodeIn(sent[Limit - 1]);

        var over = await PostFromAsync(Forgot, new { email = victim }, "2001:db8:99::1");
        AssertTooManyRequests(over);

        // The refused request issued no code, so the one the owner got last still works; and no further email went.
        await _host.CreateUserAsync("limit-barrier@novacore.test", Password);
        await PostFromAsync(Forgot, new { email = "limit-barrier@novacore.test" }, "2001:db8:100::1");
        await fixture.Emails.WaitForAsync(email => string.Equals(email.To, "limit-barrier@novacore.test", StringComparison.Ordinal));
        Assert.Equal(Limit, fixture.Emails.Sent.Count(email => string.Equals(email.To, victim, StringComparison.Ordinal)));

        using var client = _host.CreateClient();
        using var reset = await client.PostAsJsonAsync(new Uri("/api/v1/auth/password/reset", UriKind.Relative),
            new { email = victim, code = lastCode, newPassword = NewPassword }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
    }

    [Fact]
    public async Task An_address_without_an_account_gets_the_same_answers_as_one_with_an_account()
    {
        await _host.CreateUserAsync("limit-known@novacore.test", Password);

        var known = await AnswersAsync("limit-known@novacore.test");
        var unknown = await AnswersAsync("limit-nobody@novacore.test");

        Assert.Equal([.. Enumerable.Repeat(StatusCodes.Status202Accepted, Limit), StatusCodes.Status429TooManyRequests], known);
        Assert.Equal(known, unknown);

        async Task<int[]> AnswersAsync(string email)
        {
            var answers = new List<int>();
            for (var i = 0; i <= Limit; i++)
                answers.Add((await PostFromAsync(Forgot, new { email }, $"2001:db8:{200 + i}::1")).Status);
            return [.. answers];
        }
    }

    [Fact]
    public async Task The_email_endpoints_share_one_limit_per_address_whatever_its_case()
    {
        const string address = "limit-shared@novacore.test";
        await CreateUnconfirmedAsync(address);

        Assert.Equal(StatusCodes.Status200OK,
            (await PostFromAsync("/api/v1/auth/resend-confirmation-email", new { email = address }, "198.51.100.1")).Status);
        Assert.Equal(StatusCodes.Status200OK,
            (await PostFromAsync("/api/v1/auth/resend-confirmation-email", new { email = address.ToUpperInvariant() }, "198.51.100.2")).Status);

        using var browser = new OidcFlow(_host);
        using (var link = await RequestResetLinkAsync(browser, "Limit-Shared@novacore.test"))
            Assert.Equal(HttpStatusCode.OK, link.StatusCode);

        AssertTooManyRequests(await PostFromAsync("/api/v1/auth/register",
            new { email = address, password = "another long passphrase", displayName = (string?)null }, "198.51.100.3"));
        using (var page = await RequestResetLinkAsync(browser, address))
            Assert.Equal(HttpStatusCode.TooManyRequests, page.StatusCode);

        // Another address keeps its own budget.
        Assert.Equal(StatusCodes.Status200OK,
            (await PostFromAsync("/api/v1/auth/resend-confirmation-email", new { email = "limit-other@novacore.test" }, "198.51.100.3")).Status);
    }

    [Theory]
    [InlineData(257)]
    [InlineData(1_000_000)]
    public async Task The_reset_link_form_refuses_an_over_long_address_before_the_limit_counts_it(int length)
    {
        // An address no account can have: the account API's limit is 256 characters (AccountRequests). Counted, every
        // such value would hold a partition of the limit for its whole window, megabytes each (V6e-2).
        var email = new string('a', length - "@novacore.test".Length) + "@novacore.test";

        using var browser = new OidcFlow(_host);
        for (var i = 0; i <= Limit; i++)
        {
            using var page = await RequestResetLinkAsync(browser, email);
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var html = await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("Enter a valid email address.", html, StringComparison.Ordinal);
        }

        Assert.DoesNotContain(fixture.Emails.Sent, sent => string.Equals(sent.To, email, StringComparison.Ordinal));
    }

    [Fact]
    public async Task The_reset_link_form_refuses_a_malformed_address_like_the_account_api()
    {
        using var browser = new OidcFlow(_host);
        using var page = await RequestResetLinkAsync(browser, "not-an-address");

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Enter a valid email address.",
            await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public void The_limit_refuses_an_over_long_key_whatever_its_budget()
    {
        using var limit = new EmailRecipientLimit(Options.Create(new IdentityServerOptions()));
        var longest = new string('A', EmailRecipientLimit.MaxAddressLength);

        Assert.False(limit.TryAcquire(longest + "A"));
        Assert.True(limit.TryAcquire(longest));
    }

    private static void AssertTooManyRequests((int Status, string Body) response)
    {
        Assert.Equal(StatusCodes.Status429TooManyRequests, response.Status);
        using var body = JsonDocument.Parse(response.Body);
        Assert.Equal("account.too_many_emails", body.RootElement.GetProperty("code").GetString());
    }

    private static async Task<HttpResponseMessage> RequestResetLinkAsync(OidcFlow browser, string email)
    {
        using var form = await browser.Browser.GetAsync(new Uri("/account/forgot-password", UriKind.Relative));
        return await browser.SubmitFormAsync(form, "/account/forgot-password", new(StringComparer.Ordinal) { ["Email"] = email });
    }

    // HttpClient can't choose the client address, so the request goes to TestServer directly (StrictRateLimitTests).
    private async Task<(int Status, string Body)> PostFromAsync(string path, object body, string clientIp)
    {
        var json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body));
        var context = await _host.Server.SendAsync(http =>
        {
            http.Request.Method = HttpMethods.Post;
            http.Request.Scheme = "https";
            http.Request.Path = path;
            http.Request.ContentType = "application/json";
            http.Request.ContentLength = json.Length;
            http.Request.Body = new MemoryStream(json);
            http.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestHasBody());
            http.Connection.RemoteIpAddress = IPAddress.Parse(clientIp);
        });

        using var reader = new StreamReader(context.Response.Body);
        return (context.Response.StatusCode, await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
    }

    private async Task CreateUnconfirmedAsync(string email)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var created = await users.CreateAsync(
            new ApplicationUser { UserName = email, Email = email, CreatedAtUtc = TimeProvider.System.GetUtcNow() }, Password);
        Assert.True(created.Succeeded);
    }

    private static string CodeIn(SentEmail email) => Code().Match(email.HtmlBody).Groups["code"].Value;

    [GeneratedRegex(@"<strong>(?<code>\d{8})</strong>", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Code();

    private sealed class RequestHasBody : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }
}
