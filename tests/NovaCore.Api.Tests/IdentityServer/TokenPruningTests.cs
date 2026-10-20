using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace NovaCore.Api.Tests.IdentityServer;

/// <summary>
/// The provider deletes the tokens and authorizations nobody can use any more (PLAN F4d.5), every hour, so the store
/// doesn't grow forever: expired or revoked tokens and revoked authorizations without tokens, once they are older than
/// the minimum lifespan. A valid permanent authorization and a recent token stay.
/// </summary>
[Collection(IdentityServerTestGroup.Name)]
public sealed class TokenPruningTests(TokenPruningTests.Fixture fixture) : IClassFixture<TokenPruningTests.Fixture>
{
    private readonly IdentityServerTestHost _host = fixture.Host;

    /// <summary>A fake clock drives the hourly pass; this host signs nobody in, so the clock may move (tests/CLAUDE.md).</summary>
    public sealed class Fixture : IdentityServerFixture
    {
        public FakeTimeProvider Clock { get; } = new(TimeProvider.System.GetUtcNow());

        protected override void Configure(WebApplicationBuilder builder) => builder.Services.AddSingleton<TimeProvider>(Clock);
    }

    [Fact]
    public async Task An_hourly_pass_deletes_what_nobody_can_use_and_keeps_the_rest()
    {
        // OpenIddict compares the expiration with the database's clock, which is the real one (RESEARCH R-088).
        var now = TimeProvider.System.GetUtcNow();
        var old = now - TimeSpan.FromDays(30);

        string revokedAuthorization, expiredToken, validAuthorization, recentToken;
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var application = await scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>()
                .FindByClientIdAsync(IdentityServerTestHost.ClientId);
            var applicationId = await scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>().GetIdAsync(application!);
            var authorizations = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();
            var tokens = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();

            revokedAuthorization = await CreateAuthorizationAsync(authorizations, applicationId!, Statuses.Revoked, old);
            expiredToken = await CreateTokenAsync(tokens, applicationId!, revokedAuthorization, old, old + TimeSpan.FromDays(7));
            validAuthorization = await CreateAuthorizationAsync(authorizations, applicationId!, Statuses.Valid, old);
            recentToken = await CreateTokenAsync(tokens, applicationId!, validAuthorization, now, now + TimeSpan.FromDays(7));
        }

        fixture.Clock.Advance(TimeSpan.FromHours(1));

        await WaitUntilAsync(async () => !await TokenExistsAsync(expiredToken) && !await AuthorizationExistsAsync(revokedAuthorization));
        Assert.True(await AuthorizationExistsAsync(validAuthorization));
        Assert.True(await TokenExistsAsync(recentToken));
    }

    private static async Task<string> CreateAuthorizationAsync(
        IOpenIddictAuthorizationManager authorizations, string applicationId, string status, DateTimeOffset created)
    {
        var authorization = await authorizations.CreateAsync(new OpenIddictAuthorizationDescriptor
        {
            ApplicationId = applicationId,
            Subject = Guid.NewGuid().ToString(),
            Status = status,
            Type = AuthorizationTypes.Permanent,
            CreationDate = created
        });
        return (await authorizations.GetIdAsync(authorization))!;
    }

    private static async Task<string> CreateTokenAsync(
        IOpenIddictTokenManager tokens, string applicationId, string authorizationId, DateTimeOffset created, DateTimeOffset expires)
    {
        var token = await tokens.CreateAsync(new OpenIddictTokenDescriptor
        {
            ApplicationId = applicationId,
            AuthorizationId = authorizationId,
            Subject = Guid.NewGuid().ToString(),
            Status = Statuses.Valid,
            Type = TokenTypeHints.RefreshToken,
            CreationDate = created,
            ExpirationDate = expires
        });
        return (await tokens.GetIdAsync(token))!;
    }

    private async Task<bool> TokenExistsAsync(string id)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>().FindByIdAsync(id) is not null;
    }

    private async Task<bool> AuthorizationExistsAsync(string id)
    {
        await using var scope = _host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>().FindByIdAsync(id) is not null;
    }

    // The pass runs in the background after the clock moves.
    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var deadline = TimeProvider.System.GetUtcNow() + TimeSpan.FromSeconds(10);
        while (!await condition())
        {
            Assert.True(TimeProvider.System.GetUtcNow() < deadline, "The pass did not prune within 10 seconds.");
            await Task.Delay(50);
        }
    }
}
