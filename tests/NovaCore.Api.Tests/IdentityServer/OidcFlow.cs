using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Plays both sides of an Authorization Code + PKCE sign-in against <see cref="IdentityServerTestHost"/>: a browser that
/// keeps cookies and doesn't follow redirects, and the public client that redeems the code.
/// </summary>
internal sealed partial class OidcFlow : IDisposable
{
    public OidcFlow(IdentityServerTestHost host)
    {
        Browser = new HttpClient(new CookieContainerHandler { InnerHandler = host.Server.CreateHandler() })
        {
            BaseAddress = new Uri(IdentityServerTestHost.Authority)
        };
    }

    /// <summary>The browser: sends the provider's cookies, returns redirects instead of following them.</summary>
    public HttpClient Browser { get; }

    /// <summary>Signs <paramref name="email"/> in from scratch and redeems the code for tokens.</summary>
    public async Task<TokenSet> SignInAsync(string email, string password)
    {
        var (verifier, challenge) = NewPkce();

        var toLogin = await Browser.GetAsync(AuthorizeUri(challenge));
        Assert.Equal(HttpStatusCode.Redirect, toLogin.StatusCode);

        var afterLogin = await SubmitLoginAsync(await Browser.GetAsync(toLogin.Headers.Location), email, password);
        Assert.Equal(HttpStatusCode.Redirect, afterLogin.StatusCode);

        var callback = await Browser.GetAsync(afterLogin.Headers.Location);
        return await RedeemAsync(CodeFrom(callback), verifier);
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

        return await Browser.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = HiddenValue(html, "__RequestVerificationToken"),
            ["ReturnUrl"] = HiddenValue(html, "ReturnUrl"),
            ["Email"] = email,
            ["Password"] = password
        }));
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

    public Task<HttpResponseMessage> TokenRequestAsync(Dictionary<string, string> parameters)
    {
        parameters.TryAdd("client_id", IdentityServerTestHost.ClientId);
        return Browser.PostAsync("/connect/token", new FormUrlEncodedContent(parameters));
    }

    /// <summary>Calls <paramref name="path"/> with <paramref name="accessToken"/> as the bearer token, without cookies.</summary>
    public async Task<HttpResponseMessage> CallApiAsync(string path, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
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

    public void Dispose() => Browser.Dispose();

    private static string HiddenValue(string html, string name)
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
