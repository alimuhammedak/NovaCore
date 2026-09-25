using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using NovaCore.AspNetCore.Auth;
using Xunit;

namespace NovaCore.Api.Tests.Unit;

public class JwtTokenServiceTests
{
    [Fact]
    public void Tokens_are_stamped_with_the_injected_clock()
    {
        var now = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var options = Options.Create(new JwtOptions { SigningKey = new string('k', 32), AccessTokenMinutes = 15 });
        var service = new JwtTokenService(options, new FakeTimeProvider(now));

        var accessToken = service.CreateAccessToken([new Claim("sub", "42")]);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(accessToken.Token);

        Assert.Equal(now.UtcDateTime, jwt.IssuedAt);
        Assert.Equal(now.AddMinutes(15).UtcDateTime, jwt.ValidTo);
        Assert.Equal(now, accessToken.IssuedAtUtc);
        Assert.Equal(now.AddMinutes(15), accessToken.ExpiresAtUtc);
        Assert.Equal(15 * 60, accessToken.ExpiresInSeconds);
    }
}
