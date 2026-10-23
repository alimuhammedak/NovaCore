using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Plays both sides of an Authorization Code + PKCE sign-in against <see cref="IdentityServerTestHost"/>: a browser that
/// keeps cookies and doesn't follow redirects, and the public client that redeems the code.
/// </summary>
internal sealed partial class OidcFlow : IDisposable
{
    private readonly CookieContainerHandler _cookies;

    public OidcFlow(IdentityServerTestHost host)
        : this(host.Server)
    {
    }

    /// <summary>Drives the provider of any TestServer at <see cref="IdentityServerTestHost.Authority"/>, e.g. the Api host.</summary>
    public OidcFlow(TestServer server)
    {
        _cookies = new CookieContainerHandler { InnerHandler = server.CreateHandler() };
        Browser = new HttpClient(_cookies, disposeHandler: false)
        {
            BaseAddress = new Uri(IdentityServerTestHost.Authority)
        };
    }

    /// <summary>The browser: sends the provider's cookies, returns redirects instead of following them.</summary>
    public HttpClient Browser { get; }

    /// <summary>Signs <paramref name="email"/> in from scratch and redeems the code for tokens.</summary>
    public async Task<TokenSet> SignInAsync(string email, string password)
    {
        var (verifier, afterPassword) = await BeginSignInAsync(email, password);
        return await FinishSignInAsync(afterPassword, verifier);
    }

    /// <summary>Starts an authorization request and passes the password step; the response is where it leads.</summary>
    public async Task<(string Verifier, HttpResponseMessage AfterPassword)> BeginSignInAsync(string email, string password)
    {
        var (verifier, challenge) = NewPkce();

        var toLogin = await Browser.GetAsync(AuthorizeUri(challenge));
        Assert.Equal(HttpStatusCode.Redirect, toLogin.StatusCode);

        return (verifier, await SubmitLoginAsync(await Browser.GetAsync(toLogin.Headers.Location), email, password));
    }

    /// <summary>Follows the redirect back to the authorization endpoint and redeems the code it issues.</summary>
    public async Task<TokenSet> FinishSignInAsync(HttpResponseMessage backToAuthorize, string verifier)
    {
        Assert.Equal(HttpStatusCode.Redirect, backToAuthorize.StatusCode);
        Assert.StartsWith("/connect/authorize", backToAuthorize.Headers.Location!.OriginalString, StringComparison.Ordinal);

        var callback = await Browser.GetAsync(backToAuthorize.Headers.Location);
        return await RedeemAsync(CodeFrom(callback), verifier);
    }

    /// <summary>Posts the authenticator-code form of <paramref name="codePage"/>.</summary>
    public async Task<HttpResponseMessage> SubmitCodeAsync(HttpResponseMessage codePage, string code)
    {
        Assert.Equal(HttpStatusCode.OK, codePage.StatusCode);
        var html = await codePage.Content.ReadAsStringAsync();

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = HiddenValue(html, "__RequestVerificationToken"),
            ["ReturnUrl"] = HiddenValue(html, "ReturnUrl"),
            ["Code"] = code
        });
        return await Browser.PostAsync("/account/login/2fa", form);
    }

    /// <summary>
    /// Posts the form of <paramref name="page"/> to <paramref name="action"/>, as a browser submits it: every hidden field
    /// (the antiforgery token among them) plus <paramref name="fields"/>. <paramref name="configure"/> can change the
    /// request before it is sent, e.g. its Host header.
    /// </summary>
    public async Task<HttpResponseMessage> SubmitFormAsync(
        HttpResponseMessage page, string action, Dictionary<string, string> fields, Action<HttpRequestMessage>? configure = null)
    {
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();

        var values = HiddenInput().Matches(html)
            .ToDictionary(match => match.Groups["name"].Value, match => WebUtility.HtmlDecode(match.Groups["value"].Value));
        foreach (var (name, value) in fields)
            values[name] = value;

        using var request = new HttpRequestMessage(HttpMethod.Post, action) { Content = new FormUrlEncodedContent(values) };
        configure?.Invoke(request);
        return await Browser.SendAsync(request);
    }

    /// <summary>An authorization request for the test client; <paramref name="extra"/> adds or overrides parameters.</summary>
    public static string AuthorizeUri(string? codeChallenge, params (string Name, string? Value)[] extra)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["client_id"] = IdentityServerTestHost.ClientId,
            ["redirect_uri"] = IdentityServerTestHost.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid offline_access",
            ["state"] = "state-123",
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = codeChallenge is null ? null : "S256"
        };
        foreach (var (name, value) in extra)
            parameters[name] = value;

        return QueryHelpers.AddQueryString("/connect/authorize", parameters.Where(pair => pair.Value is not null));
    }

    /// <summary>Posts the sign-in form of <paramref name="loginPage"/> with its antiforgery token and return URL.</summary>
    public async Task<HttpResponseMessage> SubmitLoginAsync(HttpResponseMessage loginPage, string email, string password)
    {
        Assert.Equal(HttpStatusCode.OK, loginPage.StatusCode);
        var html = await loginPage.Content.ReadAsStringAsync();

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = HiddenValue(html, "__RequestVerificationToken"),
            ["ReturnUrl"] = HiddenValue(html, "ReturnUrl"),
            ["Email"] = email,
            ["Password"] = password
        });
        return await Browser.PostAsync("/account/login", form);
    }

    /// <summary>Exchanges an authorization code (with its PKCE verifier) at the token endpoint.</summary>
    public async Task<TokenSet> RedeemAsync(string code, string verifier)
    {
        var response = await TokenRequestAsync(new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = IdentityServerTestHost.RedirectUri,
            ["code_verifier"] = verifier
        });
        return await TokensFrom(response);
    }

    /// <summary>A refresh token request; the caller checks the response.</summary>
    public Task<HttpResponseMessage> RefreshAsync(string refreshToken) => TokenRequestAsync(new()
    {
        ["grant_type"] = "refresh_token",
        ["refresh_token"] = refreshToken
    });

    public async Task<HttpResponseMessage> TokenRequestAsync(Dictionary<string, string> parameters)
    {
        parameters.TryAdd("client_id", IdentityServerTestHost.ClientId);
        using var form = new FormUrlEncodedContent(parameters);
        return await Browser.PostAsync("/connect/token", form);
    }

    /// <summary>Calls <paramref name="path"/> with <paramref name="accessToken"/> as the bearer token.</summary>
    public async Task<HttpResponseMessage> CallApiAsync(string path, string accessToken) =>
        await CallApiAsync(HttpMethod.Get, path, accessToken, body: null);

    /// <summary>Sends <paramref name="body"/> (as JSON, when given) to <paramref name="path"/> with a bearer token.</summary>
    public async Task<HttpResponseMessage> CallApiAsync(HttpMethod method, string path, string accessToken, object? body)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return await Browser.SendAsync(request);
    }

    public static async Task<TokenSet> TokensFrom(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new TokenSet(
            json.GetProperty("access_token").GetString()!,
            json.GetProperty("refresh_token").GetString()!,
            json.TryGetProperty("id_token", out var idToken) ? idToken.GetString() : null);
    }

    public static async Task<string> ErrorFrom(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()!;

    public static string CodeFrom(HttpResponseMessage callback)
    {
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        var location = callback.Headers.Location!;
        Assert.StartsWith(IdentityServerTestHost.RedirectUri, location.ToString(), StringComparison.Ordinal);
        return QueryHelpers.ParseQuery(location.Query)["code"].ToString();
    }

    public static (string Verifier, string Challenge) NewPkce()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        return (verifier, Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
    }

    public void Dispose()
    {
        Browser.Dispose();
        _cookies.Dispose();
    }

    public static string HiddenValue(string html, string name)
    {
        var match = HiddenInput().Matches(html).FirstOrDefault(m => m.Groups["name"].Value == name);
        Assert.NotNull(match);
        return WebUtility.HtmlDecode(match.Groups["value"].Value);
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [GeneratedRegex("""<input type="hidden" name="(?<name>[^"]+)" value="(?<value>[^"]*)">""")]
    private static partial Regex HiddenInput();
}

internal sealed record TokenSet(string AccessToken, string RefreshToken, string? IdToken);
