using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The JSON sign-in a project's own screen uses (PLAN F8.3, D-091 (1), (2)): the same rules as the HTML pages
/// (<see cref="SignInFlow"/>), an antiforgery header, no cross-origin access, local return URLs only, a session cookie
/// and never a token.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class SessionSignInTests(SignInHardeningTests.Fixture fixture) : IClassFixture<SignInHardeningTests.Fixture>
{
    private const string Password = "a long passphrase for tests";
    private const string Login = "/api/v1/auth/session/login";
    private const string TwoFactor = "/api/v1/auth/session/login/2fa";

    private readonly IdentityServerTestHost _host = fixture.Host;

    [Fact]
    public async Task An_unknown_account_a_wrong_password_and_a_locked_account_get_the_same_answer()
    {
        var wrong = await _host.CreateUserAsync("session-wrong@novacore.test", Password);
        var locked = await _host.CreateUserAsync("session-locked@novacore.test", Password);
        using var browser = new OidcFlow(_host);
        for (var attempt = 0; attempt < 5; attempt++)
            await SignInAsync(browser, locked.Email!, "not the right passphrase");

        var answers = new[]
        {
            await SignInAsync(browser, "nobody-session@novacore.test", "not the right passphrase"),
            await SignInAsync(browser, wrong.Email!, "not the right passphrase"),
            await SignInAsync(browser, locked.Email!, Password),
        };

        Assert.All(answers, answer => Assert.Equal(HttpStatusCode.Unauthorized, answer.StatusCode));
        var problems = await Task.WhenAll(answers.Select(ProblemAsync));
        Assert.All(problems, problem => Assert.Equal("account.invalid_credentials", problem.Code));
        Assert.Single(problems.Select(problem => (problem.Title, problem.Detail, problem.Status)).Distinct());
    }

    [Fact]
    public async Task An_unknown_account_costs_a_password_hash_like_a_known_one()
    {
        await _host.CreateUserAsync("session-timing@novacore.test", Password);
        using var browser = new OidcFlow(_host);

        var before = fixture.PasswordChecks.Count;
        await SignInAsync(browser, "session-timing@novacore.test", "not the right passphrase");
        var known = fixture.PasswordChecks.Count - before;

        before = fixture.PasswordChecks.Count;
        await SignInAsync(browser, "nobody-session-timing@novacore.test", "not the right passphrase");
        var unknown = fixture.PasswordChecks.Count - before;

        Assert.True(known > 0);
        Assert.Equal(known, unknown);
    }

    // D-091 (1): the same body, status and time. Counted rather than timed: the hash is what takes the time (600,000
    // PBKDF2 iterations), and SignInManager answers a locked or unconfirmed account before it hashes (R-138).
    [Fact]
    public async Task Every_refusal_costs_the_same_password_hashes()
    {
        var wrong = await _host.CreateUserAsync("session-cost-wrong@novacore.test", Password);
        var locked = await _host.CreateUserAsync("session-cost-locked@novacore.test", Password);
        var unconfirmed = await CreateUnconfirmedAsync("session-cost-unconfirmed@novacore.test");
        var lockedUnconfirmed = await CreateUnconfirmedAsync("session-cost-locked-unconfirmed@novacore.test");
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var user in new[] { locked, lockedUnconfirmed })
            {
                var stored = (await users.FindByIdAsync(user.Id.ToString()))!;
                Assert.True((await users.SetLockoutEndDateAsync(stored, TimeProvider.System.GetUtcNow().AddHours(1))).Succeeded);
            }
        }
        using var browser = new OidcFlow(_host);

        // The first check for an unknown address makes the dummy hash, once per host.
        await SignInAsync(browser, "nobody-session-cost-warm-up@novacore.test", "not the right passphrase");

        var costs = new List<int>();
        foreach (var (email, password) in new[]
        {
            ("nobody-session-cost@novacore.test", "not the right passphrase"),
            (wrong.Email!, "not the right passphrase"),
            (locked.Email!, Password),
            (unconfirmed.Email!, "not the right passphrase"),
            (lockedUnconfirmed.Email!, Password),
        })
        {
            var before = fixture.PasswordChecks.Count;
            using var answer = await SignInAsync(browser, email, password);
            Assert.Equal(HttpStatusCode.Unauthorized, answer.StatusCode);
            costs.Add(fixture.PasswordChecks.Count - before);
        }

        Assert.Equal([1, 1, 1, 1, 1], costs);
    }

    [Fact]
    public async Task An_unconfirmed_account_hears_about_its_email_only_with_the_right_password()
    {
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = "session-unconfirmed@novacore.test", Email = "session-unconfirmed@novacore.test", CreatedAtUtc = TimeProvider.System.GetUtcNow() };
            Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        }
        using var browser = new OidcFlow(_host);

        var wrong = await SignInAsync(browser, "session-unconfirmed@novacore.test", "not the right passphrase");
        var right = await SignInAsync(browser, "session-unconfirmed@novacore.test", Password);

        Assert.Equal("account.invalid_credentials", (await ProblemAsync(wrong)).Code);
        Assert.Equal(HttpStatusCode.Forbidden, right.StatusCode);
        Assert.Equal("account.email_not_confirmed", (await ProblemAsync(right)).Code);
    }

    [Fact]
    public async Task A_request_without_the_antiforgery_header_is_refused()
    {
        var user = await _host.CreateUserAsync("session-csrf@novacore.test", Password);
        using var browser = new OidcFlow(_host);
        await AntiforgeryTokenAsync(browser);

        // The cookie alone, as a form on another site would send it.
        var response = await browser.Browser.PostAsJsonAsync(new Uri(Login, UriKind.Relative),
            new { email = user.Email, password = Password }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("account.antiforgery_invalid", (await ProblemAsync(response)).Code);
    }

    [Theory]
    [InlineData("https://evil.example/connect/authorize")]
    [InlineData("//evil.example/connect/authorize")]
    [InlineData("/\\evil.example")]
    public async Task A_return_url_outside_this_host_is_refused(string returnUrl)
    {
        var user = await _host.CreateUserAsync($"session-return-{Guid.NewGuid():N}@novacore.test", Password);
        using var browser = new OidcFlow(_host);

        var response = await SignInAsync(browser, user.Email!, Password, returnUrl);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("ReturnUrl", out _));
    }

    [Fact]
    public async Task A_signed_in_browser_gets_its_code_from_the_authorization_endpoint_and_no_token()
    {
        var user = await _host.CreateUserAsync("session-flow@novacore.test", Password);
        using var browser = new OidcFlow(_host);
        var (verifier, challenge) = OidcFlow.NewPkce();
        var toLogin = await browser.Browser.GetAsync(OidcFlow.AuthorizeUri(challenge), TestContext.Current.CancellationToken);
        var returnUrl = ReturnUrlOf(toLogin.Headers.Location!);

        var response = await SignInAsync(browser, user.Email!, Password, returnUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain("token", body.Replace("returnUrl", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
        using var answer = JsonDocument.Parse(body);
        Assert.Equal("signed_in", answer.RootElement.GetProperty("status").GetString());
        Assert.Equal(returnUrl, answer.RootElement.GetProperty("returnUrl").GetString());

        var callback = await browser.Browser.GetAsync(new Uri(returnUrl, UriKind.Relative), TestContext.Current.CancellationToken);
        var tokens = await browser.RedeemAsync(OidcFlow.CodeFrom(callback), verifier);
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
    }

    [Fact]
    public async Task An_account_with_two_factor_authentication_signs_in_with_its_code()
    {
        var user = await _host.CreateUserAsync("session-2fa@novacore.test", Password);
        string key;
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var stored = (await users.FindByIdAsync(user.Id.ToString()))!;
            await users.ResetAuthenticatorKeyAsync(stored);
            await users.SetTwoFactorEnabledAsync(stored, enabled: true);
            key = (await users.GetAuthenticatorKeyAsync(stored))!;
        }
        using var browser = new OidcFlow(_host);

        var password = await SignInAsync(browser, user.Email!, Password, "/connect/authorize");
        using (var answer = JsonDocument.Parse(await password.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)))
            Assert.Equal("two_factor_required", answer.RootElement.GetProperty("status").GetString());

        var wrongCode = await PostAsync(browser, TwoFactor, new { code = AuthenticatorCode.WrongFor(key) });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongCode.StatusCode);
        Assert.Equal("account.invalid_code", (await ProblemAsync(wrongCode)).Code);

        var rightCode = await PostAsync(browser, TwoFactor, new { code = AuthenticatorCode.For(key), returnUrl = "/connect/authorize" });
        Assert.Equal(HttpStatusCode.OK, rightCode.StatusCode);
        using var signedIn = JsonDocument.Parse(await rightCode.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("signed_in", signedIn.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task The_code_step_without_the_password_step_is_refused()
    {
        using var browser = new OidcFlow(_host);

        var response = await PostAsync(browser, TwoFactor, new { code = "123456" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("account.two_factor_session_missing", (await ProblemAsync(response)).Code);
    }

    [Theory]
    [InlineData("/api/v1/auth/session/antiforgery")]
    [InlineData(Login)]
    [InlineData(TwoFactor)]
    public void The_session_endpoints_use_the_strict_rate_limit(string path)
    {
        var endpoints = _host.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => string.Equals(endpoint.RoutePattern.RawText?.TrimEnd('/'), path, StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, endpoint =>
            Assert.Equal("strict", endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName));
    }

    // RFC 9700 §2.6, D-090 (3): a script on another origin mustn't sign in with, or read, the browser's session.
    [Theory]
    [InlineData("/api/v1/auth/session/antiforgery", "GET")]
    [InlineData(Login, "POST")]
    [InlineData(TwoFactor, "POST")]
    public async Task The_session_endpoints_answer_no_cross_origin_request(string path, string method)
    {
        const string origin = "https://app.novacore.test";
        await using var host = await IdentityServerTestHost.StartAsync(configure: builder =>
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { ["Cors:AllowedOrigins:0"] = origin }));
        using var client = host.CreateClient();
        using var preflight = new HttpRequestMessage(HttpMethod.Options, path);
        preflight.Headers.Add("Origin", origin);
        preflight.Headers.Add("Access-Control-Request-Method", method);

        var response = await client.SendAsync(preflight, TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private async Task<ApplicationUser> CreateUnconfirmedAsync(string email)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, CreatedAtUtc = TimeProvider.System.GetUtcNow() };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        return user;
    }

    private static async Task<HttpResponseMessage> SignInAsync(OidcFlow browser, string email, string password, string? returnUrl = null) =>
        await PostAsync(browser, Login, new { email, password, returnUrl });

    // Like a project's screen: fetch the antiforgery token from this origin, send it back in the header.
    private static async Task<HttpResponseMessage> PostAsync(OidcFlow browser, string path, object body)
    {
        var (token, header) = await AntiforgeryTokenAsync(browser);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative)) { Content = JsonContent.Create(body) };
        request.Headers.Add(header, token);
        return await browser.Browser.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<(string Token, string Header)> AntiforgeryTokenAsync(OidcFlow browser)
    {
        var response = await browser.Browser.GetAsync(new Uri("/api/v1/auth/session/antiforgery", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return (body.RootElement.GetProperty("token").GetString()!, body.RootElement.GetProperty("headerName").GetString()!);
    }

    private static async Task<Problem> ProblemAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = body.RootElement;
        return new Problem(
            root.TryGetProperty("code", out var code) ? code.GetString() : null,
            root.GetProperty("title").GetString(),
            root.TryGetProperty("detail", out var detail) ? detail.GetString() : null,
            root.GetProperty("status").GetInt32());
    }

    // The authorization endpoint sends a signed-out browser to the sign-in page with the request as ReturnUrl.
    private static string ReturnUrlOf(Uri loginPage)
    {
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(new Uri(IdentityServerTestHost.Authority), loginPage).Query);
        return query["ReturnUrl"].ToString();
    }

    private sealed record Problem(string? Code, string? Title, string? Detail, int Status);
}
