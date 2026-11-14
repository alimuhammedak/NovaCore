using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The configurable sign-in path (PLAN F8.4, D-091 (3)): <c>/connect/authorize</c> sends a signed-out browser to a
/// project's own screen when <c>Identity:SignIn:LoginPath</c> names one, to the provider's page otherwise; a path that
/// could lead off this host stops the host.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class LoginPathTests
{
    private const string Password = "a long passphrase for tests";
    private const string OwnScreen = "/sign-in";

    [Fact]
    public async Task Authorize_sends_a_signed_out_browser_to_the_configured_path_with_the_request_as_return_url()
    {
        await using var host = await StartAsync(OwnScreen);
        using var browser = new OidcFlow(host);

        var response = await browser.Browser.GetAsync(OidcFlow.AuthorizeUri(OidcFlow.NewPkce().Challenge), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = Absolute(response.Headers.Location!);
        Assert.Equal(OwnScreen, location.AbsolutePath);
        Assert.StartsWith("/connect/authorize", ReturnUrlOf(location), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_own_screen_signs_in_through_the_session_endpoint_and_authorize_then_issues_a_code()
    {
        await using var host = await StartAsync(OwnScreen);
        var user = await host.CreateUserAsync("login-path@novacore.test", Password);
        using var browser = new OidcFlow(host);
        var (verifier, challenge) = OidcFlow.NewPkce();
        var toScreen = await browser.Browser.GetAsync(OidcFlow.AuthorizeUri(challenge), TestContext.Current.CancellationToken);
        var returnUrl = ReturnUrlOf(Absolute(toScreen.Headers.Location!));

        var signIn = await SessionSignInAsync(browser, user.Email!, returnUrl);
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        using (var body = JsonDocument.Parse(await signIn.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)))
            Assert.Equal(returnUrl, body.RootElement.GetProperty("returnUrl").GetString());

        var callback = await browser.Browser.GetAsync(new Uri(returnUrl, UriKind.Relative), TestContext.Current.CancellationToken);
        var tokens = await browser.RedeemAsync(OidcFlow.CodeFrom(callback), verifier);
        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
    }

    [Fact]
    public async Task Without_the_setting_authorize_sends_the_browser_to_the_provider_page()
    {
        await using var host = await StartAsync(loginPath: null);
        using var browser = new OidcFlow(host);

        var response = await browser.Browser.GetAsync(OidcFlow.AuthorizeUri(OidcFlow.NewPkce().Challenge), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login", Absolute(response.Headers.Location!).AbsolutePath);
    }

    [Theory]
    [InlineData("https://evil.novacore.test/sign-in")]
    [InlineData("//evil.novacore.test/sign-in")]
    [InlineData("/\\evil.novacore.test/sign-in")]
    [InlineData("sign-in")]
    [InlineData("~/sign-in")]
    [InlineData("/sign-in?next=1")]
    [InlineData("/sign-in#top")]
    public async Task A_path_that_could_leave_this_host_stops_the_host(string loginPath)
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => StartAsync(loginPath));

        Assert.Contains("Identity:SignIn:LoginPath", exception.ToString(), StringComparison.Ordinal);
    }

    private static Task<IdentityServerTestHost> StartAsync(string? loginPath) =>
        IdentityServerTestHost.StartAsync(configure: builder =>
        {
            if (loginPath is not null)
                builder.Configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>(StringComparer.Ordinal) { ["Identity:SignIn:LoginPath"] = loginPath });
        });

    // Like a project's screen: fetch the antiforgery token from this origin, send it back in the header.
    private static async Task<HttpResponseMessage> SessionSignInAsync(OidcFlow browser, string email, string returnUrl)
    {
        var antiforgery = await browser.Browser.GetAsync(new Uri("/api/v1/auth/session/antiforgery", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, antiforgery.StatusCode);
        using var token = JsonDocument.Parse(await antiforgery.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/auth/session/login", UriKind.Relative))
        {
            Content = JsonContent.Create(new { email, password = Password, returnUrl })
        };
        request.Headers.Add(token.RootElement.GetProperty("headerName").GetString()!, token.RootElement.GetProperty("token").GetString());
        return await browser.Browser.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static Uri Absolute(Uri location) =>
        location.IsAbsoluteUri ? location : new Uri(new Uri(IdentityServerTestHost.Authority), location);

    private static string ReturnUrlOf(Uri location) => QueryHelpers.ParseQuery(location.Query)["ReturnUrl"].ToString();
}
