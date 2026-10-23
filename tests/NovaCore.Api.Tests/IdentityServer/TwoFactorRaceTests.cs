using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaCore.Identity.OpenIddict;
using Xunit;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// Two requests verifying one authenticator code at the same time, made to interleave on every run (<see cref="UsedCodeRace"/>):
/// both read the user before either records the code. One gets in; the other is refused like a used code, never with a 500.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class TwoFactorRaceTests(TwoFactorRaceTests.Fixture fixture) : IClassFixture<TwoFactorRaceTests.Fixture>
{
    private const string Password = "a long passphrase for tests";

    private readonly IdentityServerTestHost _host = fixture.Host;

    [Fact]
    public async Task Two_requests_enabling_two_factor_with_one_code_let_one_in()
    {
        // The account's first verified code: neither request finds a used-codes token, so both add one.
        await _host.CreateUserAsync("mfa-race-enable@novacore.test", Password);
        using var flow = new OidcFlow(_host);
        var accessToken = (await flow.SignInAsync("mfa-race-enable@novacore.test", Password)).AccessToken;
        var key = await SetUpAsync(flow, accessToken);
        var code = AuthenticatorCode.For(key);

        HttpResponseMessage? competing = null;
        fixture.Race.Arm(async () => competing = await EnableAsync(flow, accessToken, code));
        var first = await EnableAsync(flow, accessToken, code);

        Assert.Equal(HttpStatusCode.OK, competing!.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await EnableAsync(flow, accessToken, code)).StatusCode);
        Assert.True(await TwoFactorEnabledAsync("mfa-race-enable@novacore.test"));
    }

    [Fact]
    public async Task Two_sign_ins_racing_with_one_code_let_one_in()
    {
        await _host.CreateUserAsync("mfa-race@novacore.test", Password);
        using var setupFlow = new OidcFlow(_host);
        var accessToken = (await setupFlow.SignInAsync("mfa-race@novacore.test", Password)).AccessToken;
        var key = await SetUpAsync(setupFlow, accessToken);
        Assert.Equal(HttpStatusCode.OK, (await EnableAsync(setupFlow, accessToken, AuthenticatorCode.For(key))).StatusCode);
        var code = AuthenticatorCode.For(key, stepsAhead: 1);

        using var first = new OidcFlow(_host);
        using var second = new OidcFlow(_host);
        var firstCodePage = await CodeStepAsync(first, "mfa-race@novacore.test");
        var secondCodePage = await CodeStepAsync(second, "mfa-race@novacore.test");

        HttpResponseMessage? competing = null;
        fixture.Race.Arm(async () => competing = await second.SubmitCodeAsync(secondCodePage, code));
        var response = await first.SubmitCodeAsync(firstCodePage, code);

        // The competing browser records the code first and goes back to the authorization endpoint; this one gets the
        // code form again, its save stopped by the user's concurrency stamp.
        Assert.Equal(HttpStatusCode.Redirect, competing!.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Passes the password step of an account with two-factor authentication; returns the code page.
    private static async Task<HttpResponseMessage> CodeStepAsync(OidcFlow flow, string email)
    {
        var (_, afterPassword) = await flow.BeginSignInAsync(email, Password);
        Assert.StartsWith("/account/login/2fa", afterPassword.Headers.Location!.OriginalString, StringComparison.Ordinal);
        return await flow.Browser.GetAsync(afterPassword.Headers.Location);
    }

    private static Task<HttpResponseMessage> EnableAsync(OidcFlow flow, string accessToken, string code) =>
        flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/enable", accessToken, new { password = Password, code });

    private static async Task<string> SetUpAsync(OidcFlow flow, string accessToken)
    {
        var setup = await flow.CallApiAsync(HttpMethod.Post, "/api/v1/auth/2fa/setup", accessToken, new { password = Password });
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        return (await setup.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sharedKey").GetString()!;
    }

    private async Task<bool> TwoFactorEnabledAsync(string email)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await users.GetTwoFactorEnabledAsync((await users.FindByEmailAsync(email))!);
    }

    /// <summary>The shared host, with <see cref="Race"/> on its store's contexts.</summary>
    public sealed class Fixture : IdentityServerFixture
    {
        public UsedCodeRace Race { get; } = new();

        protected override void Configure(WebApplicationBuilder builder) =>
            builder.Services.ConfigureDbContext<IdentityServerDbContext>(options => options.AddInterceptors(Race));
    }
}
